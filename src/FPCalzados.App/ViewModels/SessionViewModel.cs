using FPCalzados.Domain;
using FPCalzados.Infrastructure;

namespace FPCalzados.App.ViewModels;

public sealed class SessionViewModel(ApplicationStore store) : BaseViewModel
{
    private UserRole? _currentRole;
    private bool _requiresPasswordChange;

    public UserRole? CurrentRole
    {
        get => _currentRole;
        private set => SetProperty(ref _currentRole, value);
    }

    public bool RequiresPasswordChange
    {
        get => _requiresPasswordChange;
        private set => SetProperty(ref _requiresPasswordChange, value);
    }

    public void Authenticate(UserRole role, string password)
    {
        var session = store.Authenticate(role, password);
        CurrentRole = role;
        RequiresPasswordChange = session.RequiresPasswordChange;
    }

    public void ChangeCurrentPassword(string password)
    {
        if (CurrentRole is null)
        {
            throw new InvalidOperationException("No hay una sesión activa.");
        }

        store.ChangeRolePassword(CurrentRole.Value, password);
        RequiresPasswordChange = false;
    }

    public void Logout()
    {
        CurrentRole = null;
        RequiresPasswordChange = false;
    }
}
