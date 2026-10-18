using System.Data;

namespace EventRegistration.Api.Interfaces;

public interface IEventRegistrationDatabase
{
    IDbConnection Open();
}