namespace ReLoop_Technologies_Web_App.Data;

public interface IReLoopDocumentStore
{
    Task<T?> GetAsync<T>(string collection, string documentId) where T : class;
    Task<IReadOnlyList<T>> ListAsync<T>(string collection) where T : class;
    Task SaveAsync<T>(string collection, string documentId, T document) where T : class;
    Task DeleteAsync(string collection, string documentId);
    Task<TResult> RunTransactionAsync<TResult>(Func<IReLoopDocumentTransaction, Task<TResult>> operation);
}

public interface IReLoopDocumentTransaction
{
    Task<T?> GetAsync<T>(string collection, string documentId) where T : class;
    void Set<T>(string collection, string documentId, T document) where T : class;
    void Delete(string collection, string documentId);
}
