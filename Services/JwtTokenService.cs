using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataBaseCDF.Models;
using Microsoft.IdentityModel.Tokens;

namespace DataBaseCDF.Services;

public class JwtTokenService(string secretKey)
{
    public string GenerateToken(Member member)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim("id", member.id.ToString()),
                new Claim("admin", member.admin.ToString()),
                new Claim("version", member.version.ToString())
            ]),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}