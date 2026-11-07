# Guia de construção: passo a passo

> **Atalho:** a pasta [`pbip/`](pbip/) já traz o projeto montado (modelo, medidas, relacionamentos e 4 páginas). Abra `pbip/Reclamacoes.pbip`, ajuste o parâmetro `pPastaDados` e atualize. Se der erro ao abrir, siga este guia para montar do zero e use o `pbip/` como referência.

Tempo estimado para montar do zero: 3 a 5 horas. Siga na ordem.

---

## 1. Baixar os dados

1. Baixe o arquivo (≈ 340 MB):
   https://www.anatel.gov.br/dadosabertos/paineis_de_dados/consumidor/consumidor_reclamacoes.zip
2. Descompacte em uma pasta **fora do OneDrive da empresa**, por exemplo `C:\Dados\Anatel\`.
   O zip tem dois arquivos. Use o **`reclamacoes_contexto.csv`** (2020 em diante). O `reclamacoes.csv` vai só até 2020.

## 2. Power Query

1. Power BI Desktop > **Transformar dados**.
2. **Gerenciar parâmetros > Novo**:
   - `pPastaDados` (Texto) = `C:\Dados\Anatel\` (com a barra no final)
   - `pAnoInicial` (Número decimal) = `2022`. Comece com 2 a 3 anos para o arquivo ficar leve.
3. Para cada bloco de [`power-query/consultas.pq`](power-query/consultas.pq):
   **Nova Fonte > Consulta Nula > Editor Avançado**, cole o bloco e renomeie a consulta com o nome indicado.
   Ordem: `stg_Reclamacoes` → `fReclamacoes` → `dLocal` → `dOperadora` → `dServico` → `dProblema`.
4. Na `stg_Reclamacoes`, clique com o botão direito e **desmarque "Habilitar carga"**.
5. **Fechar e Aplicar**.

> Se der erro de coluna, abra o CSV no Bloco de Notas e confira os nomes do cabeçalho. A Anatel às vezes muda acentos.

> ⚠️ **Memória:** se cada dimensão vier de uma consulta Power Query sobre o CSV, o Power BI lê o arquivo de 1,4 GB várias vezes em paralelo, e isso pode dar "Não existem recursos de sistema suficientes". O `pbip/` resolve assim: só a fato lê o CSV e as dimensões são **tabelas calculadas em DAX** (`DISTINCT` sobre as chaves da fato). Veja `pbip/Reclamacoes.SemanticModel/definition/tables/`.

## 3. Modelo

1. **Modelagem > Nova tabela**: cole a fórmula `dCalendario` de [`dax/medidas.dax`](dax/medidas.dax).
2. Renomeie `Date` para `Data` e classifique `Mes` por `MesNum`.
3. **Marcar como tabela de datas** (coluna `Data`).
4. Na Exibição de modelo, crie os 5 relacionamentos listados no arquivo DAX (todos 1:*, filtro único).
5. Crie a tabela `_Medidas` (**Inserir dados > OK**) e cole as medidas nela.
6. Formatos: `Reclamações` → número inteiro com separador; `%` → porcentagem com 1 casa.
7. Oculte as colunas-chave da fato (`CodMunicipio`, `Marca`, `Servico`, `ChaveProblema`).
8. Em `dLocal[LocalMapa]`, defina **Categoria de dados = Local**. Em `dLocal[UF]`, defina **Estado ou Província**.

## 4. Relatório

Use poucas cores: cinza para o contexto e **uma** cor de destaque (técnica do *Storytelling com Dados*).
Tamanho da página: 16:9, 1280 × 720.

### Página 1: Visão geral
| Visual | Campos |
|---|---|
| 4 cartões | `Reclamações`, `Var % Ano a Ano` (cor via `Cor Variação`), `Taxa de Reabertura`, `Média Mensal de Reclamações` |
| Linha | Eixo `dCalendario[AnoMes]` · Valores `Reclamações` e `Reclamações Média Móvel 3M` |
| Barras horizontais | `dServico[ServicoNome]` × `Reclamações` (ordenado do maior para o menor) |
| Mapa coroplético | `dLocal[UF]` × `Reclamações` |
| Segmentações | `dCalendario[Ano]`, `dServico[GrupoServico]`, `dLocal[Regiao]` |
| Título da página | Caixa de texto com o valor da medida `Título Variação` |

### Página 2: Operadoras
| Visual | Campos |
|---|---|
| Barras | `dOperadora[Marca]` × `Participação nas Reclamações` (top 8) |
| Barras | `dOperadora[Marca]` × `Reabertura vs Mercado (p.p.)` (positivo = pior que o mercado) |
| Matriz | Linhas `dOperadora[Marca]` · Colunas `dServico[ServicoNome]` · Valor `Reclamações` (com mapa de calor) |
| Linha | `dCalendario[AnoMes]` × `Reclamações` · Legenda `dOperadora[Marca]` (até 5 operadoras) |

### Página 3: Motivos
| Visual | Campos |
|---|---|
| Barras | Top 10 `dProblema[Problema]` × `Reclamações` |
| Árvore de decomposição | Analisar `Reclamações` · Explicar por `Assunto`, `ServicoNome`, `Marca`, `UF` |
| Cartão | `Título Principal Problema` |

### Página 4: Canais e atendimento
| Visual | Campos |
|---|---|
| Barras | `fReclamacoes[CanalEntrada]` × `Reclamações` |
| Colunas 100% | `dCalendario[Ano]` × `Reclamações` · Legenda `fReclamacoes[Condicao]` |

## 5. Acabamento

- [ ] Títulos de cada visual escritos como **conclusão**, e não como descrição ("Cobrança lidera…" em vez de "Reclamações por assunto").
- [ ] Rodapé com a fonte: "Fonte: Anatel, Dados Abertos. Período: [Período Selecionado]".
- [ ] Navegação entre páginas por botões.
- [ ] Dicas de ferramenta (tooltips) com `Var % Ano a Ano`.

## 6. Publicar

1. Tire prints de cada página e salve em `imagens/` (`01-visao-geral.png`, `02-operadoras.png`…).
2. Preencha a seção **Principais insights** do README com 3 a 5 conclusões reais.
3. Suba para o GitHub, no repositório `Power-BI-` ou em um novo como `powerbi-reclamacoes-anatel`:
   `README.md`, `GUIA.md`, `dax/`, `power-query/`, `imagens/` e o `.pbix`. O `.pbix` só deve subir se tiver menos de 100 MB; se passar, deixe só os prints.
4. Opcional: **Publicar na Web** pelo Power BI Service, para ter um link interativo no portfólio. Use uma conta pessoal, não a da empresa.
