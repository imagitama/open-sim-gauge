using Avalonia;
using Avalonia.Controls;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Windows.Input;

namespace OpenGaugeClient.Editor.Components
{
    public enum TransformActionType { Up, Down, Left, Right, CW, CCW, Upsize, Downsize }

    public partial class TransformControl : UserControl, INotifyPropertyChanged
    {
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        public static readonly StyledProperty<ICommand?> CommandProperty =
            AvaloniaProperty.Register<TransformControl, ICommand?>(nameof(Command));
        public static readonly StyledProperty<object?> CommandTargetProperty =
            AvaloniaProperty.Register<TransformControl, object?>(nameof(CommandTarget));
        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public object? CommandTarget
        {
            get => GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }
        public object CommandParameterUp => new { Action = TransformActionType.Up, Target = CommandTarget };
        public object CommandParameterDown => new { Action = TransformActionType.Down, Target = CommandTarget };
        public object CommandParameterLeft => new { Action = TransformActionType.Left, Target = CommandTarget };
        public object CommandParameterRight => new { Action = TransformActionType.Right, Target = CommandTarget };
        public object CommandParameterCW => new { Action = TransformActionType.CW, Target = CommandTarget };
        public object CommandParameterCCW => new { Action = TransformActionType.CCW, Target = CommandTarget };
        public object CommandParameterUpsize => new { Action = TransformActionType.Upsize, Target = CommandTarget };
        public object CommandParameterDownsize => new { Action = TransformActionType.Downsize, Target = CommandTarget };
        private readonly CompositeDisposable _cleanup = new();

        public TransformControl()
        {
            InitializeComponent();

            this.GetObservable(CommandTargetProperty)
                .Subscribe(_ => NotifyParameterChanges())
                .DisposeWith(_cleanup);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _cleanup.Dispose();
        }

        private void NotifyParameterChanges()
        {
            OnPropertyChanged(nameof(CommandParameterUp));
            OnPropertyChanged(nameof(CommandParameterDown));
            OnPropertyChanged(nameof(CommandParameterLeft));
            OnPropertyChanged(nameof(CommandParameterRight));
            OnPropertyChanged(nameof(CommandParameterCW));
            OnPropertyChanged(nameof(CommandParameterCCW));
            OnPropertyChanged(nameof(CommandParameterUpsize));
            OnPropertyChanged(nameof(CommandParameterDownsize));
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
