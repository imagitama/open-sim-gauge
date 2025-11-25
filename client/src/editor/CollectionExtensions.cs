using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;

public static class CollectionExtensions
{
    public static IObservable<NotifyCollectionChangedEventArgs> ObserveCollectionChanges(
        this INotifyCollectionChanged source)
    {
        return Observable.Create<NotifyCollectionChangedEventArgs>(obs =>
        {
            NotifyCollectionChangedEventHandler handler =
                (_, e) => obs.OnNext(e);

            source.CollectionChanged += handler;
            return Disposable.Create(() =>
                source.CollectionChanged -= handler);
        });
    }
}
