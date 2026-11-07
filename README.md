# 📡 Panorama de Reclamações em Telecom: Power BI

Dashboard em Power BI que analisa as reclamações de consumidores registradas na **Anatel** contra prestadoras de telecomunicações (telefonia móvel e fixa, banda larga e TV por assinatura), usando **dados abertos oficiais**.

> ![Visão geral do dashboard](imagens/01-visao-geral.png)
> *(substitua pelos prints do seu relatório)*

---

## 🎯 Perguntas de negócio

1. Como o volume de reclamações evolui ao longo do tempo, e em que direção está indo?
2. Quais serviços e quais operadoras concentram mais reclamações?
3. Quais são os principais motivos (assuntos e problemas) das reclamações?
4. Onde, por UF e por região, as reclamações se concentram?
5. Qual operadora tem a maior **taxa de reabertura**, ou seja, a que mais deixa problemas sem solução?

## 📊 Principais insights

1. **As reclamações caíram 41% em dois anos, mas voltaram a subir.** Foram de 2,23 milhões em 2021 para 1,31 milhão em 2023. Desde então cresceram de novo: +7,4% em 2025 e +8,2% de jan a set/2026 contra o mesmo período de 2025.

2. **A Claro passou a liderar as reclamações, e a Oi praticamente saiu do ranking.** A participação da Claro subiu de 26,3% (2021) para 33,7% (2025), à frente da Vivo (26,0%) e da TIM (21,4%). A Oi caiu de 22,6% para 4,8%, movimento que coincide com a venda da operação móvel da Oi em 2022.

3. **A banda larga fixa é o serviço que mais volta a gerar reclamações.** Entre 2023 e 2025, as reclamações de banda larga subiram 26% (374 mil para 471 mil) e quase empataram com o móvel pós-pago (490 mil). Já a telefonia fixa encolheu 74% desde 2021, acompanhando o desuso do serviço.

4. **Cobrança é o principal motivo, e cancelar ainda é difícil.** "Cobrança em desacordo com o contratado" é o problema nº 1 (8,0% das reclamações de 2025). Em seguida vêm "cancelamento solicitado por atendente não efetuado" (4,6%), "sem conexão de dados" (4,4%) e "multa por fidelização" (4,3%).

5. **A TIM resolve melhor de primeira.** Em 2025, 9,0% das reclamações da TIM foram reabertas, contra 12,0% do mercado. Claro (13,1%), Oi (12,8%) e Vivo (12,5%) ficaram acima da média.

6. **O consumidor trocou o telefone pelo digital.** O call center caiu de 39,5% para 27,8% dos registros entre 2021 e 2025. Web e app somam 69%, e o WhatsApp, que não existia como canal em 2021, já responde por 3,1%.

> Volumes absolutos, sem ajuste pelo tamanho da base de clientes de cada operadora. Uma operadora maior tende a receber mais reclamações.

---

## 🗂️ Fonte de dados

| Item | Detalhe |
|---|---|
| Fonte | [Anatel: Dados Abertos](https://www.gov.br/anatel/pt-br/dados/dados-abertos) / Painéis de Dados: Consumidor |
| Arquivo | [`consumidor_reclamacoes.zip`](https://www.anatel.gov.br/dadosabertos/paineis_de_dados/consumidor/consumidor_reclamacoes.zip) → `reclamacoes_contexto.csv` (~340 MB o zip, ~1,4 GB o CSV) |
| Granularidade | Mensal, por município, operadora, serviço e problema |
| Período | 2020 em diante (o projeto usa 2021+). O `reclamacoes.csv` do mesmo zip traz 2015–2020, com outra classificação de assuntos |
| Volume | ~9 milhões de reclamações entre jan/2021 e set/2026 |

## 🌐 Dashboard web

Além do `.pbix`, há uma versão web interativa em [`dashboard-web/`](dashboard-web/index.html), que funciona direto no navegador e no GitHub Pages:
- `Agregador.cs` lê os 8,3 milhões de linhas do CSV direto do zip e gera cubos agregados em `dados.js` (~2,3 MB).
- `index.html` traz filtros cruzados (ano, operadora, serviço, UF), KPIs, texto-resumo automático, linha mensal, participação, reabertura vs. mercado, mapa por UF, top problemas e canais, cada um com visão em tabela.

**Colunas usadas:** `SOLICITAÇÕES`, `Ano`, `Mês`, `UF`, `Cidade`, `CO_MUNICIPIO`, `CanalEntrada`, `Condição`, `TipoAtendimento`, `Serviço`, `Marca`, `Assunto`, `Problema`.

> ⚠️ Os dados brutos **não** estão no repositório por causa do tamanho. Baixe o arquivo pelo link acima.

---

## 🧱 Modelo de dados (Star Schema)

```
                 dCalendario
                      │
  dLocal ──── fReclamacoes ──── dOperadora
                 │         │
           dServico     dProblema
```

| Tabela | Tipo | Conteúdo |
|---|---|---|
| `fReclamacoes` | Fato | Data, chaves, canal, condição, tipo de atendimento, quantidade de solicitações |
| `dCalendario` | Dimensão (DAX) | Ano, mês, trimestre, semestre |
| `dLocal` | Dimensão | Município, UF, região, campo geográfico para o mapa |
| `dOperadora` | Dimensão | Operadora (marca) |
| `dServico` | Dimensão | Sigla Anatel → nome amigável (SMP, STFC, SCM, SeAC) e grupo Móvel/Fixo |
| `dProblema` | Dimensão | Hierarquia Assunto → Problema |

## 🧮 Medidas DAX em destaque

| Medida | O que mostra |
|---|---|
| `Reclamações` | Solicitações do tipo "Reclamação" |
| `Var % Ano a Ano` | Variação contra o mesmo período do ano anterior |
| `Reclamações Média Móvel 3M` | Tendência suavizada |
| `Taxa de Reabertura` | % de reclamações reabertas (problema não resolvido) |
| `Reabertura vs Mercado (p.p.)` | Operadora contra a média do mercado |
| `Participação nas Reclamações` | Fatia de cada operadora no total |
| `Principal Problema` / `Título Principal Problema` | Textos dinâmicos para os títulos-mensagem |

Todas as medidas estão em [`dax/medidas.dax`](dax/medidas.dax), e as consultas Power Query estão em [`power-query/consultas.pq`](power-query/consultas.pq).

---

## 🖥️ Páginas do relatório

1. **Visão geral:** KPIs, evolução mensal com comparação ano a ano, reclamações por serviço e mapa por UF
2. **Operadoras:** ranking, participação, taxa de reabertura contra o mercado e matriz serviço × operadora
3. **Motivos:** top assuntos e problemas, com árvore de decomposição
4. **Canais e atendimento:** canal de entrada e condição (nova, reaberta, reencaminhada)

## 🎨 Identidade visual

O dashboard web e o relatório Power BI seguem o **Padrão Digital de Governo ([Design System gov.br](https://www.gov.br/ds/))**, o mesmo usado no portal da Anatel: paleta azul (`#1351B4`, `#071D41`), cinzas neutros, verde e vermelho de feedback e tipografia Rawline/Raleway. No Power BI, o tema está em `pbip/Reclamacoes.Report/StaticResources/RegisteredResources/TemaGovBr.json`. O logotipo e as marcas oficiais não foram usados, porque o projeto é independente.

## 🛠️ Ferramentas e técnicas

`Power BI` · `Power Query (M)` · `DAX` · `Modelagem dimensional` · `Inteligência de tempo` · `Storytelling com dados`

---

## ▶️ Como reproduzir

1. Baixe e descompacte `consumidor_reclamacoes.zip` (por exemplo, em `C:\Dados\Anatel\`).
2. Abra [`pbip/Reclamacoes.pbip`](pbip/Reclamacoes.pbip) no Power BI Desktop. É um Power BI Project: modelo em TMDL e relatório em PBIR, em texto versionável.
3. Em *Transformar dados > Gerenciar parâmetros*, ajuste `pPastaDados` para a pasta do CSV e `pAnoInicial` se quiser outro ano inicial.
4. Clique em **Atualizar**. O modelo, as medidas, os relacionamentos e as 4 páginas já vêm prontos. Depois, aplique o tema e a formatação de sua preferência.

O passo a passo completo de construção está em [`GUIA.md`](GUIA.md).

## ⚖️ Observações

- A análise mostra **volumes absolutos** de reclamações. A Anatel também publica um índice relativo à base de clientes (reclamações por mil acessos), que pode virar uma evolução deste projeto cruzando com as bases de acessos.
- Projeto pessoal de estudo, feito só com dados públicos. Não representa a visão de nenhuma empresa.

---

👤 **Matheus Oliveira**: [LinkedIn](https://www.linkedin.com/in/matheus-oliveira-06b0b5162) · [GitHub](https://github.com/Maatths)
