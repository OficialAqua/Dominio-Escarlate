# ScarletCarrier 1.1.2 — patch Domínio Escarlate

Build de teste: `1.1.2-de-test.1`

Base técnica:

- ScarletCarrier `1.1.2`, commit upstream `9999b675d8ade01e47f44cb97ef7631263e33932`.
- ScarletCore `1.3.11`.
- V Rising reference package `1.1.9.9219901` resolvido pelo projeto original.
- VampireCommandFramework `0.10.4` resolvido pelo projeto original.

## Âmbito do patch

O patch não altera comandos, VRoles nem regras de autorização. Corrige apenas o ciclo de vida ECS do Carrier:

- habilita as entidades antes de preparar o inventário;
- aguarda três frames antes da primeira tentativa de inicialização;
- tenta até cinco vezes, em intervalos de três frames;
- valida `ServantEntity`, a entidade interna do inventário, `InventoryInstanceElement` e `InventoryBuffer` antes de chamar `ModifyInventorySize()`;
- regista imediatamente o Carrier criado para que o estado seja recuperável;
- cancela uma inicialização pendente quando é executado `dismiss`;
- destrói com segurança entidades recém-criadas se a inicialização falhar;
- nunca cria um Carrier novo durante `dismiss`;
- permite esconder o servant mesmo se o coffin já tiver desaparecido;
- remove o coffin órfão quando o servant já não existe.

## Instalação de teste

Não carregar esta DLL em simultâneo com a DLL original: ambas usam o mesmo GUID BepInEx e os mesmos comandos.

1. Parar completamente o servidor.
2. Fazer backup da DLL original e das configurações/dados do ScarletCarrier.
3. Retirar a DLL original da pasta carregada pelo BepInEx.
4. Copiar `ScarletCarrier-1.1.2-DE-Test.1.dll` para essa pasta.
5. Confirmar no arranque o log `version 1.1.2-de-test.1`.
6. Não alterar as regras VRoles de `VIP_Eclipse` e `VIP_Escarlate`.

## Validação no servidor

Com um jogador autorizado:

1. Executar `.carrier call` e aguardar cerca de dois segundos.
2. Abrir o inventário e confirmar os 27 slots.
3. Executar `.carrier dismiss` e aguardar cerca de dois segundos.
4. Executar `.carrier call` novamente.
5. Repetir a sequência depois de reconectar e depois de reiniciar o servidor.
6. Confirmar que não aparece `The entity does not exist` nem uma segunda entidade Carrier.

Testar também:

- `dismiss` logo depois de `call`, durante a inicialização pendente;
- `dismiss` depois de uma entidade ter sido removida/destruída;
- um utilizador sem as roles VIP, confirmando que o VRoles continua a negar o comando.

Se houver falha, guardar o bloco completo do log desde o comando até à exceção e voltar à DLL original.

## Build reproduzível

Requer um SDK .NET recente com suporte às collection expressions usadas pelo projeto e produz um assembly `net6.0`:

```powershell
dotnet restore .\ScarletCarrier.csproj
dotnet build .\ScarletCarrier.csproj -c Release
```

O `PostBuild` original contém uma cópia para um caminho pessoal Windows e pode gerar apenas um aviso noutros ambientes. A DLL compilada fica em `bin/Release/net6.0/ScarletCarrier.dll`.

Este trabalho mantém a licença AGPL-3.0 do projeto original. Ao distribuir a DLL modificada, disponibilizar também o código-fonte correspondente.
