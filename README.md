<h4 align="center">
  <br>
  <img src="https://github.com/user-attachments/assets/ff062b69-a7a1-4048-9a98-812ac31a6d42" alt="logo" width="350px"/>
  <br>
  <br>
  <br>
  Пользовательский игровой движок и редактор, созданный на C# и OpenGL, вдохновлённый архитектурой Unity.
  <br>
  <br>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4" alt=".NET 8.0" />
  <img src="https://img.shields.io/badge/Language-C%23-239120" alt="Language" />
  <img src="https://img.shields.io/badge/Version-0.1.1-ef500d" alt="Version" />
  <img src="https://img.shields.io/badge/OpenGL-4.6-4386B5" alt="OpenGL" />
<!--   <img src="https://img.shields.io/badge/Vulkan-1.3-A41E22" alt="Vulkan" />
  <img src="https://img.shields.io/badge/DirectX-9--12-80BA01" alt="DirectX" /> -->
  <br>
</h4>

![image](https://github.com/user-attachments/assets/3e3994fd-1e6a-4e18-b143-7b64c711f95a)

## Обзор

NetEngine Editor — это автономная среда разработки игр, построенная на основе фреймворка NetEngine. Он использует систему сущностей и компонентов (`GameObject`, `Component`), предлагая привычный рабочий процесс для тех, кто знаком с Unity.

> [!WARNING]
 Исходный код движка и редактора требует глубокого переосмысления и переработки. В редакторе на данный момент слишком много жёстко заданной логики и временных решений, мешающих масштабированию. В ближайшее время планирую вынести движок в отдельный репозиторий и провести полную реструктуризацию проекта с акцентом на модульность, расширяемость и чистоту архитектуры.

> [!NOTE]
> Изначально проект создавался как способ разобраться с OpenGL на практике. Многие фрагменты кода были перенесены из старых версий движка, поэтому местами могут выбиваться из общей архитектурной идеи.

## Сборка проекта

> [!IMPORTANT]
> Убедитесь, что у вас установлен .NET SDK для сборки проекта

### 1. Клонируйте репозиторий
```bash
git clone https://github.com/Kaminasta/NetEngine-Editor.git
```

### 2. Перейдите в папку репозитория
```bash
cd NetEngine-Editor
```

### 3. Соберите проект
```bash
dotnet build
```

## Лицензия

Этот проект распространяется под лицензией GNU General Public License версии 3 (GPLv3). Подробнее см. в файле LICENSE.txt.
