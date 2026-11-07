// =====================================================================
//  Panorama de Reclamações em Telecom (Anatel) — Consultas Power Query
//  Fonte: Anatel Dados Abertos — consumidor_reclamacoes.zip (reclamacoes_contexto.csv)
//
//  Como usar: no Power BI, Página Inicial > Transformar dados >
//  Nova Fonte > Consulta Nula > Editor Avançado. Cole UMA consulta por vez
//  e renomeie com o nome indicado no cabeçalho de cada bloco.
// =====================================================================


// ---------------------------------------------------------------------
// Parâmetros (crie em Gerenciar Parâmetros > Novo Parâmetro)
//   pPastaDados  (Texto)   ex.: C:\Dados\Anatel\
//   pAnoInicial  (Número)  ex.: 2022   -> limita o volume de dados
// ---------------------------------------------------------------------


// ---------------------------------------------------------------------
// stg_Reclamacoes   (botão direito > desmarcar "Habilitar carga")
// ---------------------------------------------------------------------
let
    Fonte = Csv.Document(
        // reclamacoes_contexto.csv = 2020 em diante (o reclamacoes.csv do mesmo zip vai só até 2020)
        File.Contents(pPastaDados & "reclamacoes_contexto.csv"),
        [Delimiter = ";", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    Cabecalho = Table.PromoteHeaders(Fonte, [PromoteAllScalars = true]),
    Renomeado = Table.RenameColumns(Cabecalho, {
        {"SOLICITAÇÕES", "Solicitacoes"},
        {"Mês", "Mes"},
        {"Condição", "Condicao"},
        {"Serviço", "Servico"},
        {"CO_MUNICIPIO", "CodMunicipio"}
    }),
    Tipos = Table.TransformColumnTypes(Renomeado, {
        {"Solicitacoes", Int64.Type}, {"Ano", Int64.Type}, {"Mes", Int64.Type},
        {"UF", type text}, {"Cidade", type text}, {"CodMunicipio", type text},
        {"CanalEntrada", type text}, {"Condicao", type text}, {"TipoAtendimento", type text},
        {"Servico", type text}, {"Marca", type text}, {"Assunto", type text}, {"Problema", type text}
    }),
    Filtrado = Table.SelectRows(Tipos, each [Ano] >= pAnoInicial),
    // Padroniza textos (a base mistura maiúsculas/minúsculas em alguns problemas)
    Frase = (t as nullable text) as nullable text =>
        if t = null then null
        else Text.Upper(Text.Start(Text.Trim(t), 1)) & Text.Lower(Text.Middle(Text.Trim(t), 1)),
    Limpo = Table.TransformColumns(Filtrado, {
        {"Problema", Frase, type text},
        {"Assunto", each Text.Trim(_), type text},
        {"Marca", each Text.Upper(Text.Trim(_)), type text}
    }),
    ComData = Table.AddColumn(Limpo, "Data", each #date([Ano], [Mes], 1), type date),
    ComChaveProblema = Table.AddColumn(ComData, "ChaveProblema", each [Assunto] & " | " & [Problema], type text),
    Final = Table.RemoveColumns(ComChaveProblema, {"DataExtracao", "AnoMês", "Ano", "Mes", "Linha"}, MissingField.Ignore)
in
    Final


// ---------------------------------------------------------------------
// fReclamacoes   (tabela fato — só chaves e métricas)
// ---------------------------------------------------------------------
let
    Fonte = stg_Reclamacoes,
    Colunas = Table.SelectColumns(Fonte, {
        "Data", "CodMunicipio", "Marca", "Servico", "ChaveProblema",
        "CanalEntrada", "Condicao", "TipoAtendimento", "Solicitacoes"
    })
in
    Colunas


// ---------------------------------------------------------------------
// dLocal   (UF, Região, Cidade)
// ---------------------------------------------------------------------
let
    Fonte = Table.Distinct(Table.SelectColumns(stg_Reclamacoes, {"CodMunicipio", "Cidade", "UF"}), {"CodMunicipio"}),
    Regioes = [
        AC = "Norte", AM = "Norte", AP = "Norte", PA = "Norte", RO = "Norte", RR = "Norte", TO = "Norte",
        AL = "Nordeste", BA = "Nordeste", CE = "Nordeste", MA = "Nordeste", PB = "Nordeste",
        PE = "Nordeste", PI = "Nordeste", RN = "Nordeste", SE = "Nordeste",
        DF = "Centro-Oeste", GO = "Centro-Oeste", MS = "Centro-Oeste", MT = "Centro-Oeste",
        ES = "Sudeste", MG = "Sudeste", RJ = "Sudeste", SP = "Sudeste",
        PR = "Sul", RS = "Sul", SC = "Sul"
    ],
    ComRegiao = Table.AddColumn(Fonte, "Regiao", each
        if [UF] = null then "Não informado" else Record.FieldOrDefault(Regioes, [UF], "Não informado"), type text),
    // Ajuda o visual de mapa a localizar a cidade corretamente
    ComLocalMapa = Table.AddColumn(ComRegiao, "LocalMapa", each [Cidade] & ", " & [UF] & ", Brasil", type text)
in
    ComLocalMapa


// ---------------------------------------------------------------------
// dOperadora
// ---------------------------------------------------------------------
let
    Fonte = Table.Distinct(Table.SelectColumns(stg_Reclamacoes, {"Marca"})),
    Ordenado = Table.Sort(Fonte, {{"Marca", Order.Ascending}})
in
    Ordenado


// ---------------------------------------------------------------------
// dServico   (traduz as siglas da Anatel)
// ---------------------------------------------------------------------
let
    Fonte = Table.Distinct(Table.SelectColumns(stg_Reclamacoes, {"Servico"})),
    Nomes = [
        #"SMP_Pós" = "Telefonia Móvel Pós-paga",
        #"SMP_Pré" = "Telefonia Móvel Pré-paga",
        STFC = "Telefonia Fixa",
        SCM = "Banda Larga Fixa",
        SeAC = "TV por Assinatura"
    ],
    ComNome = Table.AddColumn(Fonte, "ServicoNome", each
        if [Servico] = null then "Não informado" else Record.FieldOrDefault(Nomes, [Servico], [Servico]), type text),
    ComGrupo = Table.AddColumn(ComNome, "GrupoServico", each
        if [Servico] = null then "Outros"
        else if Text.StartsWith([Servico], "SMP") then "Móvel"
        else if List.Contains({"STFC", "SCM", "SeAC"}, [Servico]) then "Fixo"
        else "Outros", type text)
in
    ComGrupo


// ---------------------------------------------------------------------
// dProblema   (Assunto > Problema — hierarquia para drill-down)
// ---------------------------------------------------------------------
let
    Fonte = Table.Distinct(Table.SelectColumns(stg_Reclamacoes, {"ChaveProblema", "Assunto", "Problema"}), {"ChaveProblema"})
in
    Fonte
