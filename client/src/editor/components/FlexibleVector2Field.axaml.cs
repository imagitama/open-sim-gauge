using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace OpenGaugeClient.Editor.Components
{
    public partial class FlexibleVector2Field : UserControl
    {
        public static readonly StyledProperty<FlexibleVector2> ValueProperty =
            AvaloniaProperty.Register<FlexibleVector2Field, FlexibleVector2>(nameof(Value));

        public FlexibleVector2 Value
        {
            get => new FlexibleVector2() { X = X ?? "", Y = Y ?? "" };
            set
            {
                SetCurrentValue(XProperty, value.X);
                SetCurrentValue(YProperty, value.Y);
                SetCurrentValue(ValueProperty, value);
            }
        }

        public static readonly StyledProperty<object?> XProperty =
            AvaloniaProperty.Register<FlexibleVector2Field, object?>(nameof(X));

        public static readonly StyledProperty<object?> YProperty =
            AvaloniaProperty.Register<FlexibleVector2Field, object?>(nameof(Y));

        public object? X
        {
            get => GetValue(XProperty);
            set => SetValue(XProperty, value);
        }

        public object? Y
        {
            get => GetValue(YProperty);
            set => SetValue(YProperty, value);
        }

        public event Action<FlexibleVector2>? ValueCommitted;
        private readonly CompositeDisposable _cleanup = new();

        public FlexibleVector2Field()
        {
            InitializeComponent();

            XBox.Bind(TextBox.TextProperty, new Binding("X")
            {
                Mode = BindingMode.TwoWay,
                Source = this
            });

            YBox.Bind(TextBox.TextProperty, new Binding("Y")
            {
                Mode = BindingMode.TwoWay,
                Source = this
            });

            XBox.KeyDown += OnKeyDownCommit;
            YBox.KeyDown += OnKeyDownCommit;

            XBox.GetObservable(TextBox.TextProperty).Subscribe(_ => OnChanged()).DisposeWith(_cleanup);
            YBox.GetObservable(TextBox.TextProperty).Subscribe(_ => OnChanged()).DisposeWith(_cleanup);

            this.GetObservable(ValueProperty).Subscribe(OnValueChanged).DisposeWith(_cleanup);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _cleanup.Dispose();
        }

        private void OnValueChanged(FlexibleVector2? newValue)
        {
            if (newValue?.X?.ToString() != XBox.Text)
                XBox.Text = newValue?.X?.ToString();
            if (newValue?.Y?.ToString() != YBox.Text)
                YBox.Text = newValue?.Y?.ToString();
        }

        private void UpdateValue()
        {
            var xRaw = XBox.Text;
            var yRaw = YBox.Text;
            var x = xRaw ?? "";
            var y = yRaw ?? "";

            Console.WriteLine($"[FlexibleVector2Field] UpdateValue {Value} => {x},{y}");

            Value = new FlexibleVector2() { X = x, Y = y };
        }

        private void OnChanged()
        {
            X = XBox.Text;
            Y = YBox.Text;
            UpdateValue();
        }

        private void OnKeyDownCommit(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Console.WriteLine($"[FlexibleVector2Field] Commit value={Value}");
                ValueCommitted?.Invoke(Value);
            }
        }
    }
}
