namespace OrderHub.Domain.Models
{
    public class Session(Token token)
    {
        public Token Token { get; } = token;
    }
}
