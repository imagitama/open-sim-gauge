using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ReactiveUI;

namespace OpenGaugeClient.Editor.Components;

public partial class StringListField : UserControl
{
    public static readonly StyledProperty<ObservableCollection<string>> ValuesProperty =
        AvaloniaProperty.Register<StringListField, ObservableCollection<string>>(
            nameof(Values));

    public ObservableCollection<string> Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    private ObservableCollection<string>? _currentValues;
    private bool _isRebuildingProjection;

    public ObservableCollection<IndexedString> ValuesWithIndex { get; } = new();

    public ReactiveCommand<Unit, Unit> AddCommand { get; }
    public ReactiveCommand<int, Unit> DeleteCommand { get; }
    public ReactiveCommand<int, Unit> MoveUpCommand { get; }
    public ReactiveCommand<int, Unit> MoveDownCommand { get; }

    public Action<IList<string>> ValuesCommitted;

    public StringListField()
    {
        InitializeComponent();

        AddCommand = ReactiveCommand.Create(Add);
        DeleteCommand = ReactiveCommand.Create<int>(Delete);
        MoveUpCommand = ReactiveCommand.Create<int>(MoveUp);
        MoveDownCommand = ReactiveCommand.Create<int>(MoveDown);

        this.GetObservable(ValuesProperty).Subscribe(values =>
        {
            if (_currentValues != null)
                _currentValues.CollectionChanged -= OnValuesChanged;

            _currentValues = values ?? new ObservableCollection<string>();
            _currentValues.CollectionChanged += OnValuesChanged;

            RebuildProjection();
        });
    }

    private void OnValuesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isRebuildingProjection)
            return;

        RebuildProjection();
    }

    private void RebuildProjection()
    {
        _isRebuildingProjection = true;

        ValuesWithIndex.Clear();

        for (int i = 0; i < _currentValues!.Count; i++)
        {
            var index = i;
            var item = new IndexedString(index, _currentValues[i]);
            ValuesWithIndex.Add(item);
        }

        _isRebuildingProjection = false;
    }

    private void Add()
    {
        _currentValues!.Add(string.Empty);
    }

    private void Delete(int index)
    {
        if (index >= 0 && index < _currentValues!.Count)
            _currentValues.RemoveAt(index);
    }

    private void MoveUp(int index)
    {
        if (index > 0)
        {
            (_currentValues![index - 1], _currentValues[index]) =
                (_currentValues[index], _currentValues[index - 1]);
        }
    }

    private void MoveDown(int index)
    {
        if (index < _currentValues!.Count - 1)
        {
            (_currentValues[index + 1], _currentValues[index]) =
                (_currentValues[index], _currentValues[index + 1]);
        }
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SyncBackToValues();
            ValuesCommitted?.Invoke(Values);
            e.Handled = true;
        }
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is IndexedString item)
        {
            var newValue = item.Value;

            if (item.Index < _currentValues!.Count &&
                _currentValues[item.Index] != newValue)
            {
                _currentValues[item.Index] = newValue;
            }
        }
    }

    private void SyncBackToValues()
    {
        if (_currentValues == null)
            return;

        _isRebuildingProjection = true;

        _currentValues.Clear();

        foreach (var item in ValuesWithIndex)
            _currentValues.Add(item.Value);

        _isRebuildingProjection = false;
    }
}

public class IndexedString : ReactiveObject
{
    public int Index { get; }

    private string _value;
    public string Value
    {
        get => _value;
        set
        {
            this.RaiseAndSetIfChanged(ref _value, value);
            ValueChanged?.Invoke(value);
        }
    }

    public event Action<string>? ValueChanged;

    public IndexedString(int index, string value)
    {
        Index = index;
        _value = value;
    }
}
