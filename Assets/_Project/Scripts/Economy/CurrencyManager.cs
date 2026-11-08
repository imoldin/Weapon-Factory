using WeaponFactory.Core;

namespace WeaponFactory.Economy
{
    /// <summary>
    /// Facade over the coin wallet stored in the save data (GameManager persists it).
    /// UI listens to GameEvents.CoinsChanged for updates.
    /// </summary>
    public static class CurrencyManager
    {
        public static int Coins { get { return GameManager.Instance != null ? GameManager.Instance.Coins : 0; } }

        public static void Add(int amount)
        {
            if (GameManager.Instance != null) GameManager.Instance.AddCoins(amount);
        }

        public static bool CanAfford(int price) { return price >= 0 && Coins >= price; }

        public static bool TrySpend(int price)
        {
            return GameManager.Instance != null && GameManager.Instance.TrySpend(price);
        }
    }
}
