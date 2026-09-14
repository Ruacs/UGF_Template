namespace GameFramework.ObjectPool
{
    public interface IPoolable
    {
        void OnSpawn();   // 取出
        void OnRecycle(); // 回收
    }

}