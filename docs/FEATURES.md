# Funcionalidades e estado

## Implementado

- Manifesto versionado de ficheiros.
- Validação por SHA-256 suportada pelo formato.
- Distribuição do ambiente BepInEx.
- Catálogo público de plugins.
- Metadados de componentes oficiais.
- Compatibilidade publicada para plugins selecionados.

## Em desenvolvimento

- Publicação e distribuição automática do VesperClient.
- Consolidação dos catálogos e componentes oficiais.

## Inconsistências conhecidas

`official-components.json` marca atualmente o VesperClient como `required: true`, embora a decisão arquitetural seja mantê-lo opcional. Esta inconsistência deve ser tratada numa tarefa funcional separada.

## Planeado

Apenas itens confirmados por documentação ou issues devem entrar nesta secção.

## Desativado

O VesperClient aparece com `published: false` no estado analisado.
