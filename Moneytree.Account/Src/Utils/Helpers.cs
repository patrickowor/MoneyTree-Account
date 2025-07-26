namespace Moneytree.Account.Src.Utils;
using System.Security.Cryptography;
using Moneytree.Account.Src.Config;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Moneytree.Account.Src.Internal.Schemas;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

public class Helpers(EnvSchema _env)
{
    private EnvSchema env = _env;

    public String GenerateKey(String? prefix = null, String? token = null)
    {
        String parsedKey;
        if (token == null)
        {
            var key = Guid.NewGuid();
            parsedKey = key.ToString().Replace("-", "");
        }
        else
        {
            parsedKey = token;
        }

        if (prefix == null)
        {
            return parsedKey;
        }
        else
        {
            return $"{prefix}:{parsedKey}";
        }
    }
    public string GenerateNTokens(int n = 10)
    {
        var token = this.GenerateKey();
        if (n > token.Length) return token.ToUpper();
        return token.ToUpper().AsSpan(0, n).ToString();
    }
    public String GenerateOtp(int byte_length = 4)
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[byte_length]; // 4 bytes = 32 bits
        rng.GetBytes(bytes);
        int number = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // make non-negative
        return (number % 1_000_000).ToString("D6"); // ensures 6 digits with leading zeros
    }

    public string GenerateJwtToken(string userId, JwtTokenEnum tokenType)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(env.JWT_SECRET));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim("user_id", userId),
            new Claim("token_type", tokenType.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: env.JWT_ISSUER,
            audience: env.JWT_AUDIENCE,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(env.JWT_EXPIRY),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    public ExpandoObject OmitFields(object source, params string[] fieldsToOmit)
    {
        IDictionary<string, object?> expando = new ExpandoObject();
        foreach (PropertyInfo prop in source.GetType().GetProperties())
        {
            if (!fieldsToOmit.Contains(prop.Name))
            {
                expando[prop.Name] = prop.GetValue(source);
            }
        }
        return (ExpandoObject)expando;
    }

};
