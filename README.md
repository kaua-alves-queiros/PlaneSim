# PlaneSim - 3D Flight Simulator

**PlaneSim** é um simulador de voo técnico desenvolvido em C# com **MonoGame (DesktopGL)**. O projeto foca em física aerodinâmica vetorial precisa e um visual minimalista e elegante em estilo *low-poly*.

---

## 🚀 Funcionalidades Principais

*   **Física de Voo Realista**: Simulação 3D de forças reais (Empuxo, Arrasto Parasita e Induzido, Sustentação por Ângulo de Ataque e Flaps, Gravidade).
*   **Geometria Procedural (Low-Poly)**: 
    *   Montanhas geradas por ondas senoidais sobrepostas com cores baseadas em altitude.
    *   Um vale desobstruído (corredor de decolagem) gerado através da distância ao segmento de reta da pista.
    *   Pista de pouso com faixas de limite e linhas tracejadas.
    *   Aeronave modelada por vértices com hélice rotativa responsiva à potência do motor.
    *   Sombreamento de faces (*flat-shaded*) recalculado dinamicamente para manter a estética geométrica e volumétrica.
*   **Duas Perspectivas de Câmera**: 
    *   **Terceira Pessoa (Chase)**: Câmera com amortecimento de inércia e inclinação que acompanha o banking do avião.
    *   **Primeira Pessoa (Cockpit)**: Posicionada na cabine, alinhada com o nariz do avião, exibindo uma mira do diretor de voo.
*   **Telemetry HUD Glass Cockpit**: Painel translúcido exibindo velocidade em nós e km/h, altitude em pés e metros, potência do motor, indicador visual de flaps e ângulo de ataque (AoA) com alerta visual intermitente de estol (*STALL*).

---

## 🕹️ Controles

Os comandos foram mapeados simulando os controles de um manche real e pedais de leme:

| Comando | Teclas | Descrição |
| :--- | :--- | :--- |
| **Manche - Arfagem (Pitch)** | `Seta Cima` / `Seta Baixo` | Levanta (pull) ou abaixa (push) o nariz (Invertido como em aviões reais). |
| **Manche - Rolagem (Roll)** | `Seta Esquerda` / `Seta Direita` | Inclina as asas para esquerda e direita para fazer curvas. |
| **Pedais - Guinada (Yaw)** | `Teclas A` / `D` | Controla o leme de cauda para alinhar o nariz. |
| **Motor (Throttle)** | `Teclas W` / `S` | Aumenta ou diminui a potência do motor (0% a 100%). |
| **Flaps** | `Tecla F` | Alterna estágios de flaps para decolagem/pouso (0° ➔ 15° ➔ 30°). |
| **Câmera** | `Tecla C` | Alterna entre visual 3ª pessoa e Cockpit. |
| **Reiniciar** | `Tecla R` | Restaura instantaneamente o voo na cabeceira da pista. |
| **Menu / Sair** | `Tecla ESC` | Volta para o menu ou sai do voo. |

---

## 🛠️ Como Compilar e Executar

### Pré-requisitos
*   **SDK do .NET 9.0** instalado no sistema.
*   Bibliotecas do MonoGame (restauradas automaticamente durante o build).

### Passos
1. Abra o terminal na pasta do projeto:
   ```bash
   cd PlaneSim
   ```
2. Restaure e compile o executável:
   ```bash
   dotnet build
   ```
3. Execute o simulador:
   ```bash
   dotnet run
   ```

---

## 📂 Estrutura de Arquivos

```
PlaneSim/
├── Game1.cs (Inicialização e laço de jogos)
├── Program.cs (Ponto de entrada)
├── PlaneSim.csproj
└── Scripts/
    ├── Core/
    │   └── GameManager.cs (Gerenciamento de Estados e Shader Principal)
    ├── Physics/
    │   ├── AerodynamicsController.cs (Cálculo de forças e integrações)
    │   ├── EngineController.cs (Empuxo e aceleração do motor)
    │   └── ControlSurfacesController.cs (Estágios de flaps e superfícies)
    ├── Input/
    │   └── FlightInputActions.cs (Leitura de teclado e flags de clique)
    ├── Camera/
    │   └── CameraController.cs (Câmeras com lag e ponto de mira)
    ├── UI/
    │   ├── MainMenuController.cs (Menu inicial e tela de ajuda)
    │   └── FlightHUDController.cs (Painéis translúcidos e alertas)
    └── Rendering/
        ├── MeshBuilder.cs (Matrizes procedurais e flat normals)
        ├── VertexPositionNormalColor.cs (Definição de vértice do shader)
        └── PixelFontRenderer.cs (Desenho de caracteres sem dependência de SpriteFont)
```
