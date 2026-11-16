namespace NewsApi.Configuration;

public class JwtOptions
{
    public int TokenExpiration { get; set; } = 60;
}