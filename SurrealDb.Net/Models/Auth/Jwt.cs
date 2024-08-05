namespace SurrealDb.Net.Models.Auth;

/// <summary>
/// A JSON Web Token for authenticating with the server.<br /><br />
///
/// This class represents a JSON Web Token (JWT) that can be used for authentication purposes.<br /><br />
///
/// However, you should take care to ensure that only authorized users have access to the JWT.<br />
/// For example:<br />
/// * it can be stored in a secure cookie,<br />
/// * stored in a database with restricted access,<br />
/// * or encrypted in conjunction with other encryption mechanisms.<br />
/// </summary>
public readonly struct Jwt
{
    /// <summary>
    /// The underlying token string
    /// </summary>
    public string Token { get; }

    public Jwt()
    {
        Token = string.Empty;
    }

    public Jwt(string token)
    {
        Token = token;
    }

    public override string ToString()
    {
        return Token;
    }
}
