using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using Avalonia.Layout;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Disposables;

namespace OpenGaugeClient.Editor.Components
{
    public enum FieldType
    {
        Auto,
        Text,
        Bool,
        Number,
        FlexibleVector2,
        Color,
        JsonFile,
        ImageFile,
        SvgFile,
        FontFile,
        VarConfig,
        FlexibleDimension
    }

    public partial class EditableField : UserControl
    {
        public static new readonly StyledProperty<object?> ContentProperty =
            AvaloniaProperty.Register<EditableField, object?>(nameof(Content
            ));
        public new object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }
        public static readonly StyledProperty<object?> OriginalObjProperty =
            AvaloniaProperty.Register<EditableField, object?>(nameof(OriginalObj));
        public object? OriginalObj
        {
            get => GetValue(OriginalObjProperty);
            set => SetValue(OriginalObjProperty, value);
        }
        public static readonly StyledProperty<FieldType> FieldTypeProperty =
            AvaloniaProperty.Register<EditableField, FieldType>(nameof(FieldType), FieldType.Auto);
        public FieldType FieldType
        {
            get => GetValue(FieldTypeProperty);
            set => SetValue(FieldTypeProperty, value);
        }
        private object? _editValue;
        public object? EditValue
        {
            get => _editValue;
            set => SetAndRaise(EditValueProperty, ref _editValue, value);
        }
        public static readonly DirectProperty<EditableField, object?> EditValueProperty =
            AvaloniaProperty.RegisterDirect<EditableField, object?>(
            nameof(EditValue),
            o => o.EditValue,
            (o, v) => o.EditValue = v);
        public static readonly StyledProperty<string?> HintProperty =
            AvaloniaProperty.Register<EditableField, string?>(nameof(Hint
            ));
        public string? Hint
        {
            get => GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }
        private bool _isEditing;
        private Control? _editor;
        private TextBlock? _viewer;
        public static readonly StyledProperty<string?> LabelProperty =
            AvaloniaProperty.Register<EditableField, string?>(nameof(Label));
        public static readonly StyledProperty<ICommand> EditCommittedCommandProperty =
            AvaloniaProperty.Register<EditableField, ICommand>(nameof(EditCommittedCommand));
        public static readonly StyledProperty<string?> FieldNameProperty =
            AvaloniaProperty.Register<EditableField, string?>(nameof(FieldName));
        public static readonly StyledProperty<bool?> ReadOnlyProperty =
            AvaloniaProperty.Register<EditableField, bool?>(nameof(ReadOnly));
        public bool? ReadOnly
        {
            get => GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }
        public ICommand EditCommittedCommand
        {
            get => GetValue(EditCommittedCommandProperty);
            set => SetValue(EditCommittedCommandProperty, value);
        }
        public string? FieldName
        {
            get => GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }
        public string? Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
        private readonly CompositeDisposable _cleanup = new();

        public EditableField()
        {
            InitializeComponent();

            var button = this.FindControl<PrimaryButton>("EditButton") ?? throw new Exception("No button");
            var presenter = this.FindControl<ContentPresenter>("Presenter") ?? throw new Exception("No presenter");

            button.Command = ReactiveCommand.Create(() => Toggle(presenter));

            Hydrate(presenter);

            this.GetObservable(ContentProperty)
                .Subscribe(_ =>
                {
                    try
                    {
                        Hydrate(presenter);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to hydrate: {ex}");
                        throw;
                    }
                })
                .DisposeWith(_cleanup);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _cleanup.Dispose();
        }

        private void Hydrate(ContentPresenter presenter)
        {
            _editValue = Content switch
            {
                ICloneable cloneable => cloneable.Clone(),
                _ => Content
            };

            if (_isEditing)
            {
                // wont re-use viewer as complex to update
                _editor = CreateEditor();
                presenter.Content = _editor;

                if (_editor is TextBox tb)
                {
                    tb.Focus();
                    tb.CaretIndex = tb.Text?.Length ?? 0;
                }
            }
            else
            {
                if (_viewer == null)
                {
                    _viewer = CreateViewer();
                    presenter.Content = _viewer;
                }
                else
                {
                    _viewer.Text = GetLabel();
                    ToolTip.SetTip(_viewer, GetToolTip());
                    presenter.Content = _viewer;
                }
            }
        }

        private void Toggle(ContentPresenter presenter)
        {
            Console.WriteLine($"[EditableField.{FieldName}] Toggle");

            _isEditing = !_isEditing;

            Hydrate(presenter);
        }

        private TextBlock CreateViewer()
        {
            if (_viewer != null)
                return _viewer;

            var label = GetLabel();

            var textBlock = new TextBlock
            {
                Text = label.Replace("_", "__"),
                VerticalAlignment = VerticalAlignment.Center
            };

            ToolTip.SetTip(textBlock, GetToolTip());

            return textBlock;
        }

        private string GetLabel()
        {
            switch (FieldType)
            {
                case FieldType.JsonFile:
                case FieldType.ImageFile:
                case FieldType.SvgFile:
                case FieldType.FontFile:
                    if (string.IsNullOrEmpty(Content as string))
                        return "(nothing)";
                    return PathHelper.GetShortFileName((Content as string)!);
                case FieldType.VarConfig:
                    if (Content is VarConfig a)
                    {
                        var name = a.Name;
                        if (name.Length > 10)
                            name = $"{name[..10]}...";
                        return $"{name}\n{a.Unit}";
                    }
                    else
                    {
                        return "-";
                    }
                case FieldType.Bool:
                    if (Content is bool ischecked)
                        return ischecked ? "True" : "False";
                    else
                        return "null";
                default:
                    return Content?.ToString()?.Replace("_", "__") ?? "-";
            }
        }

        private string GetToolTip()
        {
            switch (FieldType)
            {
                case FieldType.JsonFile:
                case FieldType.ImageFile:
                case FieldType.SvgFile:
                case FieldType.FontFile:
                    if (string.IsNullOrEmpty(Content as string))
                        return "(nothing)";
                    return (Content as string)!;
                case FieldType.VarConfig:
                    if (Content is VarConfig a)
                        return $"{a.Name} ({a.Unit})";
                    else
                        return "-";
                case FieldType.Bool:
                    if (Content is bool ischecked)
                        return ischecked ? "True" : "False";
                    else
                        return "null";
                default:
                    return Content?.ToString() ?? "-";
            }
        }

        private string[] GetAllowedExtensions()
        {
            switch (FieldType)
            {
                case FieldType.JsonFile:
                    return ["json"];
                case FieldType.ImageFile:
                    return ["png", "jpeg", "jpg", "svg"];
                case FieldType.SvgFile:
                    return ["svg"];
                case FieldType.FontFile:
                    return ["tff"];
                default:
                    throw new Exception($"Unknown field type '{FieldType}'");
            }
        }

        private Control CreateEditor()
        {
            var fieldType = FieldType;
            var value = Content is BindingNotification bn ? bn.Value : Content;
            if (value == AvaloniaProperty.UnsetValue)
                value = null;

            if (fieldType == FieldType.Auto)
            {
                if (value is bool) fieldType = FieldType.Bool;
                else if (value is double or float or int or decimal) fieldType = FieldType.Number;
                else fieldType = FieldType.Text;
            }

            Control editor;

            switch (fieldType)
            {
                case FieldType.Bool:
                    var cb = new CheckBox();
                    cb.Bind(CheckBox.IsCheckedProperty, new Binding(nameof(EditValue))
                    {
                        Mode = BindingMode.TwoWay,
                        Source = this,
                        Converter = Converters.BoolNullableConverter.Instance
                    });

                    editor = cb;

                    cb.GetObservable(CheckBox.IsCheckedProperty)
                        .Skip(1)
                        .Subscribe(_ => CommitEdit()).DisposeWith(_cleanup);
                    break;

                case FieldType.Number:
                    var nb = new TextBox { MinWidth = 100 };
                    nb.Bind(TextBox.TextProperty, new Binding(nameof(EditValue))
                    {
                        Mode = BindingMode.TwoWay,
                        Source = this
                    });
                    nb.Watermark = "(number)";
                    nb.KeyDown += (_, e) =>
                    {
                        if (e.Key == Key.Enter) { CommitEdit(); e.Handled = true; }
                        else if (e.Key == Key.Escape) { CancelEdit(); e.Handled = true; }
                    };
                    editor = nb;
                    break;

                case FieldType.FlexibleDimension:
                    var flexibleDimensionTextBox = new TextBox { MinWidth = 100 };
                    flexibleDimensionTextBox.Bind(TextBox.TextProperty, new Binding(nameof(EditValue))
                    {
                        Mode = BindingMode.TwoWay,
                        Source = this,
                        Converter = Converters.FlexibleDimensionConverter.Instance
                    });
                    flexibleDimensionTextBox.Watermark = "(dimension)";
                    flexibleDimensionTextBox.KeyDown += (_, e) =>
                    {
                        if (e.Key == Key.Enter) { CommitEdit(); e.Handled = true; }
                        else if (e.Key == Key.Escape) { CancelEdit(); e.Handled = true; }
                    };
                    editor = flexibleDimensionTextBox;
                    break;

                case FieldType.Color:
                    {
                        var field = new ColorPickerField();
                        field.Bind(ColorPickerField.ValueProperty, new Binding(nameof(EditValue))
                        {
                            Mode = BindingMode.TwoWay,
                            Source = this,
                            Converter = Converters.ColorDefConverter.Instance
                        });

                        field.ColorCommitted += _ => CommitEdit();

                        editor = field;
                        break;
                    }

                case FieldType.JsonFile:
                case FieldType.ImageFile:
                case FieldType.SvgFile:
                case FieldType.FontFile:
                    {
                        var field = new SelectFileField()
                        {
                            AllowedExtensions = GetAllowedExtensions()
                        };
                        field.Bind(SelectFileField.PathProperty, new Binding(nameof(EditValue))
                        {
                            Mode = BindingMode.TwoWay,
                            Source = this
                        });

                        field.FileCommitted += _ => CommitEdit();

                        editor = field;
                        break;
                    }

                case FieldType.VarConfig:
                    {
                        var field = new SimVarField()
                        {

                        };
                        field.Bind(SimVarField.ValueProperty, new Binding(nameof(EditValue))
                        {
                            Mode = BindingMode.TwoWay,
                            Source = this
                        });

                        field.VarConfigCommitted += _ => CommitEdit();

                        editor = field;
                        break;
                    }

                case FieldType.FlexibleVector2:
                    {
                        var vectorField = new FlexibleVector2Field();

                        vectorField.Bind(FlexibleVector2Field.ValueProperty, new Binding(nameof(EditValue))
                        {
                            Mode = BindingMode.TwoWay,
                            Source = this
                        });

                        vectorField.ValueCommitted += _ => CommitEdit();

                        editor = vectorField;
                        break;
                    }

                default:
                    var tb = new TextBox { MinWidth = 100 };
                    tb.Bind(TextBox.TextProperty, new Binding(nameof(EditValue))
                    {
                        Mode = BindingMode.TwoWay,
                        Source = this
                    });
                    tb.KeyDown += (_, e) =>
                    {
                        if (e.Key == Key.Enter) { CommitEdit(); e.Handled = true; }
                        else if (e.Key == Key.Escape) { CancelEdit(); e.Handled = true; }
                    };
                    if (value == null) tb.Watermark = "(null)";
                    editor = tb;
                    break;
            }


            Console.WriteLine($"[EditableField.{FieldName}] Created editor '{editor}' with content='{Content}' editValue='{EditValue}'");

            return editor;
        }

        private void CommitEdit()
        {
            Console.WriteLine($"[EditableField.{FieldName}] Commit edit editValue='{EditValue}' cmd={EditCommittedCommand}");

            if (!_isEditing) return;

            Content = _editValue;
            _isEditing = false;
            _editValue = null;

            var presenter = this.FindControl<ContentPresenter>("Presenter")!;
            Hydrate(presenter);

            var payload = OriginalObj != null ? (FieldName, Content, OriginalObj) : (FieldName, Content, null);

            if (EditCommittedCommand == null)
                throw new Exception("No EditCommittedCommand");

            if (EditCommittedCommand.CanExecute(payload) == false)
            {
                throw new Exception($"Command cannot be executed: {EditCommittedCommand}");
            }

            EditCommittedCommand.Execute(payload);
        }

        private void CancelEdit()
        {
            if (!_isEditing) return;
            _isEditing = false;
            _editValue = null;

            var presenter = this.FindControl<ContentPresenter>("Presenter")!;
            _viewer = CreateViewer();
            presenter.Content = _viewer;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
