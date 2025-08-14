using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpectriumLive.Wpf.ViewModels.Base;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if ( EqualityComparer<T>.Default.Equals(backingStore, value) )
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);

        return true;
    }
}
