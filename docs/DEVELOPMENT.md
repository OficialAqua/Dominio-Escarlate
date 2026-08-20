# Manutenção e validação

## Antes de alterar um manifesto

1. Identifique o artefacto e a licença.
2. Confirme se é obrigatório ou opcional.
3. Confirme o caminho relativo de instalação.
4. Gere SHA-256 a partir do ficheiro final.
5. Use uma URL HTTPS estável.
6. Valide o JSON.
7. Teste com uma instalação descartável do V Rising.
8. Confirme instalação, atualização e remoção de ficheiros obsoletos.

## Validação local

PowerShell:

```powershell
Get-FileHash .\ficheiro.dll -Algorithm SHA256
Get-Content .\manifest.json | ConvertFrom-Json | Out-Null
```

## Segurança

Não incluir:

- assemblies proprietários do jogo;
- saves;
- tokens ou chaves;
- configurações pessoais;
- logs;
- caches;
- artefactos sem origem/licença verificável.

## Publicação

`main` representa o estado consumível. Mudanças devem ser revistas numa branch e publicadas apenas depois de os URLs e hashes estarem disponíveis.
