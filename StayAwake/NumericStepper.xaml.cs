using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StayAwake;

public partial class NumericStepper : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(NumericStepper),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(NumericStepper), new PropertyMetadata(0));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumericStepper), new PropertyMetadata(100));

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(nameof(Step), typeof(int), typeof(NumericStepper), new PropertyMetadata(1));

    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.Register(
            nameof(MaxLength),
            typeof(int),
            typeof(NumericStepper),
            new PropertyMetadata(4, OnMaxLengthChanged));

    public NumericStepper()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public int Step
    {
        get => (int)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    private static void OnMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumericStepper stepper)
            stepper.PART_TextBox.MaxLength = (int)e.NewValue;
    }

    private void StepUpButton_Click(object sender, RoutedEventArgs e) => ApplyStep(+1);

    private void StepDownButton_Click(object sender, RoutedEventArgs e) => ApplyStep(-1);

    private void NumericStepper_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!IsEnabled)
            return;

        if (e.Delta > 0)
            ApplyStep(+1);
        else if (e.Delta < 0)
            ApplyStep(-1);

        e.Handled = true;
    }

    private void PART_TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case System.Windows.Input.Key.Up:
                ApplyStep(+1);
                e.Handled = true;
                break;
            case System.Windows.Input.Key.Down:
                ApplyStep(-1);
                e.Handled = true;
                break;
        }
    }

    private void PART_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = e.Text.Any(c => !char.IsDigit(c));

    private void ApplyStep(int direction)
    {
        var current = ParseCurrentValue();
        var step = Math.Max(1, Step);
        var next = Math.Clamp(current + direction * step, Minimum, Maximum);
        Text = next.ToString(CultureInfo.InvariantCulture);
        GetBindingExpression(TextProperty)?.UpdateSource();
        PART_TextBox.Focus();
        PART_TextBox.SelectAll();
    }

    private int ParseCurrentValue()
    {
        if (int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return Math.Clamp(parsed, Minimum, Maximum);

        return Minimum;
    }
}
