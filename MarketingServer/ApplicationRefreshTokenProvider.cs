using Microsoft.Owin.Security.Infrastructure;
using System;
using System.Threading.Tasks;

namespace MarketingServer
{
    public class ApplicationRefreshTokenProvider : AuthenticationTokenProvider
    {
        private MarketingEntities db = new MarketingEntities();

        public override async Task CreateAsync(AuthenticationTokenCreateContext context)
        {
            string refreshTokenId = Guid.NewGuid().ToString("n");


            context.Ticket.Properties.IssuedUtc = DateTime.UtcNow;
            context.Ticket.Properties.ExpiresUtc = DateTime.UtcNow.AddDays(14);

            RefreshToken token = new RefreshToken()
            {
                ID = Hashing.GetHash(refreshTokenId),
                ClientID = context.Ticket.Properties.Dictionary["ClientID"],
                Ticket = context.SerializeTicket(),
                Expires = context.Ticket.Properties.ExpiresUtc.Value.DateTime
            };


            db.RefreshTokens.Add(token);

            await db.SaveChangesAsync();

            context.SetToken(refreshTokenId);


        }

        public override async Task ReceiveAsync(AuthenticationTokenReceiveContext context)
        {
            RefreshToken token = await db.RefreshTokens.FindAsync(Hashing.GetHash(context.Token));

            if (token != null)
            {
                context.DeserializeTicket(token.Ticket);
                db.RefreshTokens.Remove(token);
                await db.SaveChangesAsync();
            }
        }
    }
}