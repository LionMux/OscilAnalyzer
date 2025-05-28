using ScottPlot;
using ScottPlot.WPF;
using System.Numerics;

public class VectorPlotter
{
    private IEnumerable<Complex> _signalA;
    private IEnumerable<Complex> _signalB;
    private IEnumerable<Complex> _signalC;
    private string? _title;
    private double _radiusAxis;
    private double _maxLenghtVector;
    public WpfPlot PlotControl { get; set; }

    public VectorPlotter(IEnumerable<Complex> signalA, IEnumerable<Complex> signalB, IEnumerable<Complex> signalC, string? title = null)
    {
        if (signalA == null || signalB == null || signalC == null)
        {
            throw new ArgumentNullException("Сигналы не могут быть null");
        }

        // Проверка на одинаковую длину последовательностей
        var lengthA = signalA.Count();
        if (lengthA != signalB.Count() || lengthA != signalC.Count())
        {
            throw new ArgumentException("Все сигналы должны иметь одинаковую длину");
        }

        _signalA = signalA;
        _signalB = signalB;
        _signalC = signalC;
        _title = title;

        PlotControl = new WpfPlot();
        CreatePlot(0); // Начальное отображение для первого момента времени
    }

    // Добавляем метод для расчета максимального радиуса в конкретный момент времени
    private double CalculateMaxRadius(int timeIndex)
    {
        timeIndex = BoundaryСonditionsForIndex(timeIndex);
        var currentValues = new[]
        {
            _signalA.ElementAt(timeIndex).Magnitude,
            _signalB.ElementAt(timeIndex).Magnitude,
            _signalC.ElementAt(timeIndex).Magnitude
        };

        return currentValues.Max() * 1.2; // Увеличиваем на 20% для отступа
    }

    public void UpdatePlot(int timeIndex)
    {
        timeIndex = BoundaryСonditionsForIndex(timeIndex);
        CreatePlot(timeIndex);
    }

    private void CreatePlot(int timeIndex)
    {
        var plt = PlotControl.Plot;
        plt.Clear();
        timeIndex = BoundaryСonditionsForIndex(timeIndex);
        // Пересчитываем максимальный радиус для текущего момента времени
        _maxLenghtVector = CalculateMaxRadius(timeIndex);
        _radiusAxis = 300;

        // Создаем полярные оси с обновленным радиусом
        var polarAxis = plt.Add.PolarAxis(_radiusAxis);
        polarAxis.Circles.ForEach(x => x.LinePattern = LinePattern.Dotted);
        polarAxis.Spokes.ForEach(x => x.LinePattern = LinePattern.Dotted);

        // Устанавливаем заголовок
        if (!string.IsNullOrEmpty(_title))
        {
            plt.Title(_title);
        }

        // Получаем значения для текущего момента времени
        var vectors = new[]
        {
            _signalA.ElementAt(timeIndex),
            _signalB.ElementAt(timeIndex),
            _signalC.ElementAt(timeIndex)
        };

        var labels = new[] { "A", "B", "C" };
        IPalette palette = new ScottPlot.Palettes.Category10();
        Coordinates center = polarAxis.GetCoordinates(0, 0);

        // Отрисовка векторов
        for (int i = 0; i < vectors.Length; i++)
        {
            var koeffScale = _radiusAxis / _maxLenghtVector;
            var magnitude = vectors[i].Magnitude * koeffScale;
            var angle = Math.Atan2(vectors[i].Imaginary, vectors[i].Real) * 180 / Math.PI;

            var point = new PolarCoordinates(
                magnitude,
                Angle.FromDegrees(angle)
            );

            Coordinates tip = polarAxis.GetCoordinates(point);
            var arrow = plt.Add.Arrow(center, tip);
            arrow.ArrowLineWidth = 0;
            arrow.ArrowFillColor = palette.GetColor(i).WithAlpha(0.7);

            plt.Add.Text(
                text: labels[i],
                x: tip.X,
                y: tip.Y
            );
        }

        PlotControl.Refresh();
    }

    private int BoundaryСonditionsForIndex(int timeIndex)
    {
        if (timeIndex < 0)
        {
            timeIndex = 0;
        }
        else if (timeIndex > _signalA.Count())
        {
            timeIndex = _signalA.Count()-1;
        }

        return timeIndex;
    }

    public int TimePointsCount => _signalA.Count();
}
