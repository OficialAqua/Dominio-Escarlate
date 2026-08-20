# AGENTS.md

## Contexto

Este repositório publica manifestos, catálogos e ficheiros consumidos pelo VesperLauncher. Uma alteração incorreta pode afetar instalações existentes.

## Regras obrigatórias

1. Leia `docs/ARCHITECTURE.md` e `docs/DEVELOPMENT.md`.
2. Não altere código de outros componentes neste repositório.
3. Preserve compatibilidade retroativa dos schemas JSON.
4. Não altere caminhos geridos sem analisar remoção/migração.
5. Recalcule SHA-256 a partir do artefacto final exato.
6. Use apenas URLs HTTPS controladas ou explicitamente aprovadas.
7. Não publique tokens, passwords, webhooks privados ou configurações pessoais.
8. Não publique DLLs proprietárias do V Rising.
9. Avalie individualmente licenças de dependências redistribuíveis.
10. Não marque um componente opcional como obrigatório sem decisão explícita.
11. Valide JSON e hashes antes de publicar.
12. Atualize CHANGELOG e documentação quando o schema ou fluxo mudar.

## Branches

Apenas `main` está publicada. Trate-a como estado de distribuição. Prepare alterações de manifesto numa branch e reveja-as antes de merge.

## Relações

- VesperLauncher consome este repositório.
- VesperClient pode ser distribuído como componente opcional.
- VesperCore e PlayerServices são server-side e não pertencem ao pacote client-side salvo decisão explícita.
