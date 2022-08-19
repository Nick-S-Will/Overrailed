public interface IUpgradeable<T>
{
    public event System.Action<T> OnUpgrade;
}