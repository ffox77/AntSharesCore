using AntShares.Core;
using System;
using System.Linq;
using AntShares.Wallets;
using AntShares.Network;

namespace AntShares.Shell
{

    public class Coins
    {
        private Wallet current_wallet;
        private LocalNode local_node;

        public Coins(Wallet wallet, LocalNode node )
        {
			current_wallet = wallet;
            local_node = node;
		}

        public Fixed8 UnavailableBonus()
        {
            uint height = Blockchain.Default.Height;
            return Blockchain.CalculateBonus(current_wallet.FindUnspentCoins().Where(p => p.Output.AssetId.Equals(Blockchain.SystemShare.Hash)).Select(p => p.Reference), height);
        }

        public Fixed8 AvailableBonus()
        {

            Fixed8 bonus_available = Blockchain.CalculateBonus(current_wallet.GetUnclaimedCoins().Select(p => p.Reference));

            //if (bonus_available == Fixed8.Zero) then we can't make a claim for coins

            return bonus_available;
        }



        public bool Claim()
        {

            if( this.AvailableBonus() == Fixed8.Zero ) 
            {
                Console.WriteLine($"no coins to claim");
                return true;
            }


            CoinReference[] claims = current_wallet.GetUnclaimedCoins().Select(p => p.Reference).ToArray();
            if (claims.Length == 0) return false;


            ClaimTransaction tx = new ClaimTransaction
            {
                Claims = claims,
                Attributes = new TransactionAttribute[0],
                Inputs = new CoinReference[0],
                Outputs = new[]
                {
                    new TransactionOutput
                    {
                        AssetId = Blockchain.SystemCoin.Hash,
                        Value = Blockchain.CalculateBonus(claims),
                        ScriptHash = current_wallet.GetChangeAddress()
                    }
                }

            };

            SignTransaction(tx);

            return false;
        }


		private void SignTransaction(Transaction tx)
		{
			if (tx == null)
			{
				Console.WriteLine($"insufficient Funds");
				return;
			}
			SignatureContext context;
			try
			{
				context = new SignatureContext(tx);
			}
			catch (InvalidOperationException)
			{
				Console.WriteLine($"unsynchronized block");

				return;
			}

			current_wallet.Sign(context);

			if (context.Completed)
			{
				context.Verifiable.Scripts = context.GetScripts();
				current_wallet.SaveTransaction(tx);
                local_node.Relay(tx);
				Console.WriteLine($"Transaction Suceeded: {tx.Hash.ToString()}");
			}
			else
			{
				Console.WriteLine($"Incomplete Signature: {context.ToString()}");

			}
		}    
    }

}
    