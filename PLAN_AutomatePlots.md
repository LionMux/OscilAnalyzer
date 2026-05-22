# План: Автоматизация отображения сигналов на графике (ScottPlot)

## Текущее состояние

- Пользователь вручную выбирает 6 каналов (IA, IB, IC, UA, UB, UC) из выпадающих списков (`ComboBox`).
- Для каждого сигнала создаётся отдельный экземпляр `Plotter` → отдельный `WpfPlot`.
- В XAML 6 `ContentControl` выводят графики в `UniformGrid` (6 строк, прокрутка).
- Используется `Add.Scatter()` — медленно для больших осциллограмм.

---

## Цель

Сократить ручные действия пользователя и улучшить производительность отображения:
1. **Автоматически** сопоставлять каналы COMTRADE с фазами A/B/C по имени и единицам измерения.
2. Выводить все сигналы на **один** график (`WpfPlot`) с цветовой легендой и чекбоксами видимости.
3. Использовать `Add.Signal()` вместо `Add.Scatter()` для повышения производительности.
4. Добавить **вторую ось Y** (справа) для напряжений, чтобы не смешивать масштабы с токами.

---

## Этап 1. Автоматическое распознавание каналов

### Где изменять
`ViewModels/CometradeParserViewModel.cs`

### Что делать
После чтения `_reader.Config.AnalogChannels` автоматически подбирать каналы для свойств `CurrentAName` ... `VoltageCName`.

### Алгоритм
```
Для каждого канала из AnalogChannels:
    Привести Name и Units к нижнему регистру
    
    Если Units == "a" и Name содержит {"ia","i_a","cura","curr_a","ток a"} → CurrentAName
    Если Units == "a" и Name содержит {"ib","i_b","curb","curr_b","ток b"} → CurrentBName
    Если Units == "a" и Name содержит {"ic","i_c","curc","curr_c","ток c"} → CurrentCName
    
    Если Units == "v" и Name содержит {"ua","u_a","volta","напряжение a"} → VoltageAName
    Если Units == "v" и Name содержит {"ub","u_b","voltb","напряжение b"} → VoltageBName
    Если Units == "v" и Name содержит {"uc","u_c","voltc","напряжение c"} → VoltageCName
```

### Результат
- Пользователь открывает файл → каналы сразу подставлены в ComboBox.
- Если автоопределение не сработало (редкие имена), пользователь поправляет вручную.

---

## Этап 2. Рефакторинг Plotter → OscillogramPlotter

### Новый класс: `Instruments/OscillogramPlotter.cs`

Заменяет старый `Plotter.cs`.

```csharp
using ScottPlot;
using ScottPlot.WPF;

namespace OscilAnalyzer
{
    public class OscillogramPlotter
    {
        public WpfPlot PlotControl { get; } = new();
        
        private readonly Dictionary<string, ScottPlot.Plottables.Signal> _signals = new();
        private readonly ScottPlot.AxisPanels.AxisBase _leftAxis;
        private readonly ScottPlot.AxisPanels.AxisBase _rightAxis;

        public OscillogramPlotter()
        {
            _leftAxis = PlotControl.Plot.YAxis;
            _rightAxis = PlotControl.Plot.Axes.AddRightAxis();
            
            PlotControl.Plot.ShowLegend();
            PlotControl.Plot.XLabel("time, ms");
            PlotControl.Plot.YLabel("I, A");
            PlotControl.Plot.Axes.Right.Label.Text = "U, V";
        }

        public void AddSignal(string name, double[] x, double[] y, string unit)
        {
            if (_signals.TryGetValue(name, out var existing))
            {
                PlotControl.Plot.Remove(existing);
                _signals.Remove(name);
            }

            var sig = PlotControl.Plot.Add.Signal(y, x[1] - x[0], x[0]);
            sig.LegendText = name;
            sig.LineWidth = 1.5f;

            // Напряжения — на правую ось
            if (unit.Equals("v", StringComparison.OrdinalIgnoreCase))
            {
                sig.Axes.YAxis = _rightAxis;
            }
            else
            {
                sig.Axes.YAxis = _leftAxis;
            }

            _signals[name] = sig;
            PlotControl.Refresh();
        }

        public void SetSignalVisible(string name, bool visible)
        {
            if (_signals.TryGetValue(name, out var sig))
            {
                sig.IsVisible = visible;
                PlotControl.Refresh();
            }
        }

        public void Clear()
        {
            PlotControl.Plot.Clear();
            _signals.Clear();
            PlotControl.Refresh();
        }
    }
}
```

### Удалить
- `Instruments/Plotter.cs` (устаревший).

---

## Этап 3. Изменение ViewModel

### Файл: `ViewModels/CometradeParserViewModel.cs`

1. **Заменить 6 свойств `Plotter` на одно:**
   ```csharp
   private OscillogramPlotter _oscillogramPlotter;
   public OscillogramPlotter OscillogramPlotter
   {
       get => _oscillogramPlotter;
       set => SetProperty(ref _oscillogramPlotter, value);
   }
   ```

2. **Добавить свойства-флаги видимости для чекбоксов:**
   ```csharp
   private bool _showIA = true;
   public bool ShowIA { get => _showIA; set { SetProperty(ref _showIA, value); UpdateVisibility(); } }
   // ... аналогично для IB, IC, UA, UB, UC
   ```

3. **Метод `Plot()` заменить на:**
   ```csharp
   private void Plot()
   {
       OscillogramPlotter.Clear();
       OscillogramPlotter.AddSignal(CurrentAName, _signalDataService.TimeValues.ToArray(), _signalDataService.CurrentA.ToArray(), "A");
       OscillogramPlotter.AddSignal(CurrentBName, _signalDataService.TimeValues.ToArray(), _signalDataService.CurrentB.ToArray(), "A");
       // ... и т.д.
   }
   ```

4. **Метод автоопределения каналов (вызывать в `ReadSignal`):**
   ```csharp
   private void AutoMapChannels()
   {
       var patterns = new Dictionary<string, (string[] names, string unit)>
       {
           [nameof(CurrentAName)] = (new[]{"ia","i_a","cura","ток a"}, "a"),
           [nameof(CurrentBName)] = (new[]{"ib","i_b","curb","ток b"}, "a"),
           [nameof(CurrentCName)] = (new[]{"ic","i_c","curc","ток c"}, "a"),
           [nameof(VoltageAName)] = (new[]{"ua","u_a","volta","напр a"}, "v"),
           [nameof(VoltageBName)] = (new[]{"ub","u_b","voltb","напр b"}, "v"),
           [nameof(VoltageCName)] = (new[]{"uc","u_c","voltc","напр c"}, "v"),
       };

       foreach (var ch in _analogChanells)
       {
           var nameLo = ch.Name?.ToLowerInvariant() ?? "";
           var unitLo = ch.Units?.ToLowerInvariant() ?? "";

           foreach (var (prop, (names, expectedUnit)) in patterns)
           {
               if (unitLo == expectedUnit && names.Any(n => nameLo.Contains(n)))
               {
                   switch (prop)
                   {
                       case nameof(CurrentAName): CurrentAName = ch.Name; break;
                       case nameof(CurrentBName): CurrentBName = ch.Name; break;
                       case nameof(CurrentCName): CurrentCName = ch.Name; break;
                       case nameof(VoltageAName): VoltageAName = ch.Name; break;
                       case nameof(VoltageBName): VoltageBName = ch.Name; break;
                       case nameof(VoltageCName): VoltageCName = ch.Name; break;
                   }
               }
           }
       }
   }
   ```

---

## Этап 4. Изменение View (XAML)

### Файл: `Views/CometradeParserView.xaml`

1. **Убрать 6 `ContentControl` и `UniformGrid`.**
2. **Добавить один `WpfPlot` и панель чекбоксов:**

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>  <!-- Панель управления -->
        <RowDefinition Height="*"/>      <!-- График -->
        <RowDefinition Height="Auto"/>  <!-- Кнопки -->
    </Grid.RowDefinitions>

    <!-- Панель выбора каналов (ComboBox оставляем, но теперь с авто-подстановкой) -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
        <!-- ... текущие ComboBox для IA..UC ... -->
    </StackPanel>

    <!-- Чекбоксы видимости -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" HorizontalAlignment="Right" Margin="10">
        <CheckBox Content="IA" IsChecked="{Binding ShowIA}" Margin="5,0"/>
        <CheckBox Content="IB" IsChecked="{Binding ShowIB}" Margin="5,0"/>
        <CheckBox Content="IC" IsChecked="{Binding ShowIC}" Margin="5,0"/>
        <CheckBox Content="UA" IsChecked="{Binding ShowUA}" Margin="5,0"/>
        <CheckBox Content="UB" IsChecked="{Binding ShowUB}" Margin="5,0"/>
        <CheckBox Content="UC" IsChecked="{Binding ShowUC}" Margin="5,0"/>
    </StackPanel>

    <!-- Один график вместо шести -->
    <ScottPlot:WpfPlot Grid.Row="1" 
                       x:Name="MainPlot"
                       Content="{Binding OscillogramPlotter.PlotControl, Mode=OneWay}"/>

    <!-- Кнопки (без изменений) -->
    <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Center">
        <Button Command="{Binding StartRead}" Content="Считать осциллограмму" ... />
        <Button Command="{Binding SelectSignal}" Content="Применить выбранные сигналы" ... />
        <Button Command="{Binding MoveToNextCommand}" Content="Далее" ... />
    </StackPanel>
</Grid>
```

---

## Этап 5. Обновление AnalizeOscillogramViewModel (если нужно)

Если на экране анализа тоже есть графики, проверить, что они не зависят от старого `Plotter.cs`.
Векторные диаграммы (`VectorPlotter`) остаются без изменений — они используют полярные оси.

---

## Этап 6. Тестирование

| Сценарий | Ожидаемый результат |
|----------|---------------------|
| Открыть `Files/25_newRTDS.cfg` | Каналы IA..UC автоматически подставлены в ComboBox |
| Нажать «Применить» | Один график, 6 линий, легенда справа/сверху |
| Снять галку «IA» | Линия IA скрыта, остальные видны |
| Прокрутить / зуммировать | Все сигналы движутся синхронно |
| Повторно нажать «Применить» | Старые сигналы заменяются, утечек памяти нет |
| Открыть файл с нестандартными именами | ComboBox пусты, пользователь выбирает вручную |

---

## Риски и ограничения

| Риск | Митигация |
|------|-----------|
| `Add.Signal` требует равномерный шаг по X | COMTRADE обычно равномерная дискретизация; если нет — оставить `Add.Scatter` с предупреждением |
| Масштабы тока и напряжения сильно различаются | Правая ось Y для напряжений решает проблему |
| Старые имена каналов не распознаются | Расширить список паттернов или дать пользователю настройки |
| Легенда перекрывает график | `PlotControl.Plot.Legend.Alignment = Alignment.UpperRight;` |

---

## Итог

- **Меньше кода:** ~60 строк `Plotter.cs` + ~6 свойств VM → один класс `OscillogramPlotter`.
- **Меньше действий пользователя:** автоопределение каналов.
- **Быстрее отрисовка:** `Signal` вместо `Scatter`.
- **Удобнее UI:** один интерактивный график вместо 6 прокручиваемых.
