# IosProbe

Projeto **mínimo** de teste. Não tem nada do jogo Mini Campus. Serve para responder uma pergunta:
**Godot 4.7.1 + C# (NativeAOT) exporta e compila para iOS?**

Ele verifica, no boot, o que o jogo faz: JSON por source generator, leitura de `res://` com `FileAccess`, gravação em `user://`,
thread de trabalho, `ResourceLoader.Load` síncrono e textura criada de dados ASTC. O resultado aparece na tela e no log
(`[PROBE] RESULT: PASS`).

## Como ler o resultado do teste de compilação
Aba **Actions** do repositório, workflow **iOS compile probe**:

- Todos os passos verdes e o artefato `ios-probe-resultados` com `IosProbe-unsigned.ipa` = **compila**.
- Passo vermelho = a falha mostra onde (export do Godot, publish NativeAOT ou Xcode). Os logs completos ficam no artefato.

Isso prova a **compilação**, não a **execução**. Para ver rodando é preciso um iPhone (ver docs do Mini Campus:
`Docs/Decisoes/2026-10-03-plataformas-alvo.md`).
