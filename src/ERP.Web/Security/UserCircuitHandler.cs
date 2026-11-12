using ERP.Application.Common.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ERP.Web.Security;

/// <summary>
/// Menyinkronkan ClaimsPrincipal dari AuthenticationStateProvider ke ICurrentUserService
/// pada saat koneksi Blazor Interactive Server (SignalR Circuit) dibuka atau berubah.
/// </summary>
public sealed class UserCircuitHandler : CircuitHandler, IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ICurrentUserService _currentUserService;

    public UserCircuitHandler(
        AuthenticationStateProvider authenticationStateProvider,
        ICurrentUserService currentUserService)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _currentUserService = currentUserService;
        _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserService.SetCurrentUserContext(state.User);
        await base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserService.SetCurrentUserContext(state.User);
        await base.OnConnectionUpAsync(circuit, cancellationToken);
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            var state = await task;
            _currentUserService.SetCurrentUserContext(state.User);
        }
        catch
        {
            // Ignore background state change errors if circuit is disposing
        }
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}

