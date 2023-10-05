using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine;

public interface IHasDataBlobs
{
    public void SetDataBlob(BaseDataBlob dataBlob);
    public T GetDataBlob<T>() where T : BaseDataBlob;
    public bool TryGetDatablob<T>(out T value) where T : BaseDataBlob;
}