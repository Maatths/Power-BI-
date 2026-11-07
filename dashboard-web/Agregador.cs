using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

public static class Agregador
{
    // \u00cdndices das colunas do reclamacoes.csv
    const int C_SOL = 1, C_ANO = 2, C_MES = 3, C_UF = 5, C_CANAL = 8, C_COND = 9, C_TIPO = 10,
              C_SERV = 11, C_MARCA = 12, C_ASSUNTO = 13, C_PROB = 14;

    static string Frase(string t)
    {
        t = (t ?? "").Trim();
        if (t.Length == 0) return "N\u00e3o informado";
        return t.Substring(0, 1).ToUpper(new CultureInfo("pt-BR")) + t.Substring(1).ToLower(new CultureInfo("pt-BR"));
    }

    class Idx
    {
        public List<string> Lista = new List<string>();
        Dictionary<string, int> map = new Dictionary<string, int>();
        public int Get(string s) { int i; if (!map.TryGetValue(s, out i)) { i = Lista.Count; Lista.Add(s); map[s] = i; } return i; }
    }

    static string J(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }
    static string JList(IEnumerable<string> l) { return "[" + string.Join(",", l.Select(J)) + "]"; }

    public static string Run(string zipPath, string outPath, int anoInicial, int topMarcas, int topProblemas)
    {
        var ci = CultureInfo.InvariantCulture;
        var mes = new Idx(); var marca = new Idx(); var serv = new Idx(); var uf = new Idx();
        var canal = new Idx(); var cond = new Idx(); var assunto = new Idx(); var prob = new Idx();
        var probAssunto = new Dictionary<int, int>();
        // chave detalhada: mes, marca, serv, uf, canal, cond, prob  -> soma
        var det = new Dictionary<long, long>();
        long linhas = 0, usadas = 0, ruins = 0;
        var debug = new SortedDictionary<string, long>();

        using (var zip = ZipFile.OpenRead(zipPath))
        {
            var entry = zip.Entries.First(e => e.Name.Equals("reclamacoes_contexto.csv", StringComparison.OrdinalIgnoreCase));
            using (var sr = new StreamReader(entry.Open(), Encoding.UTF8))
            {
                sr.ReadLine(); // cabe\u00e7alho
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    linhas++;
                    var f = line.Split(';');
                    if (f.Length < 15) { ruins++; continue; }
                    int ano, m; long sol;
                    if (!int.TryParse(f[C_ANO], out ano) || !int.TryParse(f[C_MES], out m) || !long.TryParse(f[C_SOL], out sol)) { ruins++; continue; }
                    string dk = ano + ":" + (f[C_TIPO].Normalize() == "Reclama\u00e7\u00e3o"); long dv; debug.TryGetValue(dk, out dv); debug[dk] = dv + 1;
                    if (f[C_TIPO].Normalize() != "Reclama\u00e7\u00e3o") continue;
                    if (ano < anoInicial) continue;
                    usadas++;
                    int iMes = mes.Get(ano.ToString("0000") + "-" + m.ToString("00"));
                    int iMarca = marca.Get((f[C_MARCA] ?? "").Trim().ToUpperInvariant());
                    int iServ = serv.Get(f[C_SERV].Trim());
                    int iUf = uf.Get(f[C_UF].Trim());
                    int iCanal = canal.Get(f[C_CANAL].Trim());
                    int iCond = cond.Get(f[C_COND].Trim());
                    int iAss = assunto.Get(f[C_ASSUNTO].Trim());
                    int iProb = prob.Get(Frase(f[C_PROB]) + "\u0001" + iAss);
                    probAssunto[iProb] = iAss;
                    // bits: mes 8 | marca 13 | serv 6 | uf 6 | canal 6 | cond 5 | prob 13  (= 57)
                    if (iMes >= 256 || iMarca >= 8192 || iServ >= 64 || iUf >= 64 || iCanal >= 64 || iCond >= 32 || iProb >= 8192)
                        throw new Exception(string.Format("Cardinalidade: mes={0} marca={1} serv={2} uf={3} canal={4} cond={5} prob={6}", iMes, iMarca, iServ, iUf, iCanal, iCond, iProb));
                    long key = ((long)iMes << 49) + ((long)iMarca << 36) + ((long)iServ << 30) + ((long)iUf << 24) + ((long)iCanal << 18) + ((long)iCond << 13) + (long)iProb;
                    long v; det.TryGetValue(key, out v); det[key] = v + sol;
                }
            }
        }

        if (usadas == 0) return "DEBUG " + string.Join(" ", debug.Select(kv => kv.Key + "=" + kv.Value));
        var porAno = string.Join(" ", debug.Select(kv => kv.Key + "=" + kv.Value));

        // Decodifica chaves
        var rows = det.Select(kv => {
            long k = kv.Key;
            int p = (int)(k & 8191);
            int cd = (int)((k >> 13) & 31);
            int cn = (int)((k >> 18) & 63);
            int u = (int)((k >> 24) & 63);
            int s = (int)((k >> 30) & 63);
            int mk = (int)((k >> 36) & 8191);
            int ms = (int)(k >> 49);
            return new { ms, mk, s, u, cn, cd, p, v = kv.Value };
        }).ToList();

        // Ordena meses e define anos
        var mesesOrd = mes.Lista.Select((s, i) => new { s, i }).OrderBy(x => x.s).ToList();
        var mesMap = new int[mes.Lista.Count];
        for (int i = 0; i < mesesOrd.Count; i++) mesMap[mesesOrd[i].i] = i;
        var mesesFinal = mesesOrd.Select(x => x.s).ToList();
        var anos = mesesFinal.Select(s => s.Substring(0, 4)).Distinct().ToList();
        Func<int, int> anoDe = ms => anos.IndexOf(mesesFinal[mesMap[ms]].Substring(0, 4));

        // Top marcas (resto -> OUTRAS)
        var totMarca = rows.GroupBy(r => r.mk).Select(g => new { g.Key, t = g.Sum(x => x.v) }).OrderByDescending(x => x.t).ToList();
        var marcasFinal = totMarca.Take(topMarcas).Select(x => marca.Lista[x.Key]).ToList();
        bool temOutras = totMarca.Count > topMarcas;
        if (temOutras) marcasFinal.Add("OUTRAS");
        var marcaMap = new int[marca.Lista.Count];
        for (int i = 0; i < totMarca.Count; i++) marcaMap[totMarca[i].Key] = i < topMarcas ? i : topMarcas;

        // Top problemas (resto -> "Outros problemas")
        var totProb = rows.GroupBy(r => r.p).Select(g => new { g.Key, t = g.Sum(x => x.v) }).OrderByDescending(x => x.t).ToList();
        var probFinal = totProb.Take(topProblemas).Select(x => x.Key).ToList();
        var probMap = new Dictionary<int, int>();
        for (int i = 0; i < totProb.Count; i++) probMap[totProb[i].Key] = i < topProblemas ? i : topProblemas;
        var probNomes = probFinal.Select(i => prob.Lista[i].Split('\u0001')[0]).ToList(); probNomes.Add("Outros problemas");
        var probAss = probFinal.Select(i => probAssunto[i]).ToList(); probAss.Add(-1);

        var reab = cond.Lista.IndexOf("Reaberta");

        // Cubo A: mes, marca, serv, uf -> [total, reabertas]
        var cuboA = rows.GroupBy(r => new { m = mesMap[r.ms], mk = marcaMap[r.mk], r.s, r.u })
            .Select(g => new long[] { g.Key.m, g.Key.mk, g.Key.s, g.Key.u, g.Sum(x => x.v), g.Where(x => x.cd == reab).Sum(x => x.v) })
            .OrderBy(a => a[0]).ToList();
        // Cubo B: ano, marca, serv, uf, problema -> total
        var cuboB = rows.GroupBy(r => new { a = anoDe(r.ms), mk = marcaMap[r.mk], r.s, r.u, p = probMap[r.p] })
            .Select(g => new long[] { g.Key.a, g.Key.mk, g.Key.s, g.Key.u, g.Key.p, g.Sum(x => x.v) }).ToList();
        // Cubo C: ano, marca, serv, uf, canal -> total
        var cuboC = rows.GroupBy(r => new { a = anoDe(r.ms), mk = marcaMap[r.mk], r.s, r.u, r.cn })
            .Select(g => new long[] { g.Key.a, g.Key.mk, g.Key.s, g.Key.u, g.Key.cn, g.Sum(x => x.v) }).ToList();

        var sb = new StringBuilder();
        sb.Append("// Gerado a partir de Anatel Dados Abertos (consumidor_reclamacoes.zip). Apenas TipoAtendimento = Reclama\u00e7\u00e3o.\n");
        sb.Append("window.DADOS = {\n");
        sb.Append("\"geradoEm\":" + J(DateTime.Now.ToString("yyyy-MM-dd")) + ",\n");
        sb.Append("\"meses\":" + JList(mesesFinal) + ",\n");
        sb.Append("\"anos\":" + JList(anos) + ",\n");
        sb.Append("\"marcas\":" + JList(marcasFinal) + ",\n");
        sb.Append("\"servicos\":" + JList(serv.Lista) + ",\n");
        sb.Append("\"ufs\":" + JList(uf.Lista) + ",\n");
        sb.Append("\"canais\":" + JList(canal.Lista) + ",\n");
        sb.Append("\"assuntos\":" + JList(assunto.Lista) + ",\n");
        sb.Append("\"problemas\":" + JList(probNomes) + ",\n");
        sb.Append("\"problemaAssunto\":[" + string.Join(",", probAss) + "],\n");
        Action<string, List<long[]>, bool> cubo = (nome, l, last) => {
            sb.Append("\"" + nome + "\":[");
            sb.Append(string.Join(",", l.Select(a => "[" + string.Join(",", a) + "]")));
            sb.Append("]" + (last ? "\n" : ",\n"));
        };
        cubo("a", cuboA, false);
        cubo("b", cuboB, false);
        cubo("c", cuboC, true);
        sb.Append("};\n");
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));

        return porAno + "\n" + string.Format(ci, "linhas={0} reclamacoes_usadas={1} ruins={2} meses={3} marcas={4} servicos={5} ufs={6} canais={7} condicoes=[{8}] problemas={9} cuboA={10} cuboB={11} cuboC={12} kb={13}",
            linhas, usadas, ruins, mesesFinal.Count, marca.Lista.Count, serv.Lista.Count, uf.Lista.Count, canal.Lista.Count,
            string.Join("|", cond.Lista), prob.Lista.Count, cuboA.Count, cuboB.Count, cuboC.Count, new FileInfo(outPath).Length / 1024);
    }
}
