# Domínio Escarlate — Distribuição

Repositório público de distribuição e metadados usados pelo VesperLauncher para preparar o ambiente de V Rising do jogador.

Este não é o repositório do código do VesperCore nem a fonte principal da lógica do servidor.

## Conteúdo principal

- `manifest.json`: ficheiros geridos e hashes SHA-256.
- `official-components.json`: componentes oficiais do ecossistema.
- catálogos públicos de plugins compatíveis/recomendados.
- ficheiros redistribuíveis necessários ao ambiente BepInEx.

## Consumidor

O VesperLauncher descarrega os manifestos, valida URLs e hashes, instala ou atualiza ficheiros geridos e remove ficheiros que deixem de pertencer ao pacote.

## Segurança

Alterações em manifestos podem afetar diretamente instalações dos jogadores. Consulte [AGENTS.md](AGENTS.md) e [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) antes de editar.

## Documentação

- [Arquitetura de distribuição](docs/ARCHITECTURE.md)
- [Funcionalidades e estado](docs/FEATURES.md)
- [Manutenção e validação](docs/DEVELOPMENT.md)
