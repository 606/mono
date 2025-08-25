# Se.Cli Release Guide

## Ручний реліз

### Через GitHub Actions

**Ручний реліз:**
- Перейдіть в GitHub → Actions → "Release Se.Cli (Simple)"
- Натисніть "Run workflow"
- Введіть версію (наприклад, "1.0.0")
- Опціонально вкажіть гілку (за замовчуванням: `feature/init`)
- Натисніть "Run workflow"

### Оновлення версії

Щоб оновити версію Se.Cli, відредагуйте файл `Se.Cli.csproj`:

```xml
<PropertyGroup>
  <Version>1.0.1</Version>
  <AssemblyVersion>1.0.1.0</AssemblyVersion>
  <FileVersion>1.0.1.0</FileVersion>
</PropertyGroup>
```

## Структура релізу

Кожен реліз містить:
- `se-cli.tar.gz` - архів для Linux/macOS
- `se-cli.zip` - архів для Windows
- Автоматично згенеровані нотатки релізу

## Теги

Релізи створюються з тегами у форматі: `se-cli-v{version}`

Приклади:
- `se-cli-v1.0.0`
- `se-cli-v1.0.1`

## Залежності

Se.Cli залежить від:
- `Se.Common` - спільна бібліотека

## Гілки

За замовчуванням реліз будується з гілки `feature/init`. 
Можна вказати іншу гілку при ручному запуску workflow.
