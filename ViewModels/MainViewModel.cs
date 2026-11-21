using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CareFlow.DTOs;
using CareFlow.Models;
using CareFlow.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CareFlow.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IBookingService _bookingService;

    [ObservableProperty]
    private ObservableCollection<Patient> _patients = new();

    [ObservableProperty]
    private Patient? _selectedPatient;

    [ObservableProperty]
    private DateTimeOffset? _selectedDateOffset = DateTimeOffset.Now;

    [ObservableProperty]
    private ObservableCollection<SelectableServiceViewModel> _services = new();

    [ObservableProperty]
    private ObservableCollection<Booking> _bookings = new();

    [ObservableProperty]
    private Booking? _selectedBooking;

    [ObservableProperty]
    private string _statusMessage = "Ready. Select a patient, date, and 1-3 services.";

    [ObservableProperty]
    private bool _isStatusError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _simulateFailureOnConfirm;

    // Runtime constructor with Dependency Injection
    public MainViewModel(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // Design-time constructor
    public MainViewModel()
    {
        _bookingService = null!;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadPatientsAsync();
        await RefreshAvailabilityAsync();
        await LoadBookingsAsync();
    }

    [RelayCommand]
    public async Task RefreshAvailabilityAsync()
    {
        if (_bookingService == null) return;

        IsLoading = true;
        try
        {
            var date = GetCurrentSelectedDate();
            var availabilities = await _bookingService.GetAvailabilityAsync(date);

            var selectedIds = Services.Where(s => s.IsSelected).Select(s => s.Id).ToHashSet();

            Services.Clear();
            foreach (var a in availabilities)
            {
                var vm = new SelectableServiceViewModel(a.ServiceId, a.ServiceName, a.Description, a.DailyCapacity, a.BookedCount, a.RemainingCapacity)
                {
                    IsSelected = selectedIds.Contains(a.ServiceId)
                };
                Services.Add(vm);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading services: {ex.Message}", isError: true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadPatientsAsync()
    {
        if (_bookingService == null) return;
        try
        {
            var list = await _bookingService.GetPatientsAsync();
            Patients.Clear();
            foreach (var p in list)
            {
                Patients.Add(p);
            }

            if (Patients.Count > 0 && SelectedPatient == null)
            {
                SelectedPatient = Patients[0];
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading patients: {ex.Message}", isError: true);
        }
    }

    [RelayCommand]
    public async Task LoadBookingsAsync()
    {
        if (_bookingService == null) return;
        try
        {
            var list = await _bookingService.GetRecentBookingsAsync();
            Bookings.Clear();
            foreach (var b in list)
            {
                Bookings.Add(b);
            }

            if (SelectedBooking == null && Bookings.Count > 0)
            {
                SelectedBooking = Bookings[0];
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading bookings: {ex.Message}", isError: true);
        }
    }

    [RelayCommand]
    public async Task SubmitBookingAsync()
    {
        if (_bookingService == null) return;

        // BR-01 check
        if (SelectedPatient == null)
        {
            SetStatus("BR-01 Error: Please select an existing patient.", isError: true);
            return;
        }

        var date = GetCurrentSelectedDate();
        var selectedServiceIds = Services.Where(s => s.IsSelected).Select(s => s.Id).ToList();

        // BR-02 check
        if (selectedServiceIds.Count < 1 || selectedServiceIds.Count > 3)
        {
            SetStatus($"BR-02 Error: You must select 1 to 3 distinct services. (Selected: {selectedServiceIds.Count})", isError: true);
            return;
        }

        IsLoading = true;
        try
        {
            var request = new BookingRequestDto(
                SelectedPatient.Id,
                date,
                selectedServiceIds,
                SimulateFailure: SimulateFailureOnConfirm
            );

            var result = await _bookingService.CreateBookingAsync(request);

            if (result.Success)
            {
                SetStatus($"✓ {result.Message} (Booking #{result.Booking?.Id} for {SelectedPatient.FullName})", isError: false);

                // Clear selection
                foreach (var s in Services)
                {
                    s.IsSelected = false;
                }

                // Refresh capacities and history
                await RefreshAvailabilityAsync();
                await LoadBookingsAsync();
            }
            else
            {
                SetStatus($"✗ {result.Message}", isError: true);
                await RefreshAvailabilityAsync();
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Unexpected error: {ex.Message}", isError: true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedDateOffsetChanged(DateTimeOffset? value)
    {
        _ = RefreshAvailabilityAsync();
    }

    private DateOnly GetCurrentSelectedDate()
    {
        var dt = SelectedDateOffset?.DateTime ?? DateTime.Today;
        return DateOnly.FromDateTime(dt);
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
