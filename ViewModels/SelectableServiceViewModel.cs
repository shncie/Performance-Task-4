using CommunityToolkit.Mvvm.ComponentModel;

namespace CareFlow.ViewModels;

public partial class SelectableServiceViewModel : ObservableObject
{
    public int Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int DailyCapacity { get; }

    [ObservableProperty]
    private int _bookedCount;

    [ObservableProperty]
    private int _remainingCapacity;

    [ObservableProperty]
    private bool _isSelected;

    public bool IsAvailable => RemainingCapacity > 0;

    public string AvailabilityText => $"{RemainingCapacity} / {DailyCapacity} spots left ({BookedCount} booked)";

    public SelectableServiceViewModel(int id, string name, string description, int dailyCapacity, int bookedCount, int remainingCapacity)
    {
        Id = id;
        Name = name;
        Description = description;
        DailyCapacity = dailyCapacity;
        _bookedCount = bookedCount;
        _remainingCapacity = remainingCapacity;
    }

    public void UpdateCapacity(int bookedCount, int remainingCapacity)
    {
        BookedCount = bookedCount;
        RemainingCapacity = remainingCapacity;
        OnPropertyChanged(nameof(IsAvailable));
        OnPropertyChanged(nameof(AvailabilityText));
    }
}