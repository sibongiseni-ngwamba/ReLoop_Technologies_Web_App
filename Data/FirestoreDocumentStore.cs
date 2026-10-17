using Google.Cloud.Firestore;

namespace ReLoop_Technologies_Web_App.Data;

public sealed class FirestoreDocumentStore(FirestoreDb database) : IReLoopDocumentStore
{
    public async Task<T?> GetAsync<T>(string collection, string documentId) where T : class
    {
        var snapshot = await database.Collection(collection).Document(documentId).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<T>() : null;
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(string collection) where T : class
    {
        var snapshot = await database.Collection(collection).GetSnapshotAsync();
        return snapshot.Documents.Where(document => document.Exists).Select(document => document.ConvertTo<T>()).ToArray();
    }

    public async Task SaveAsync<T>(string collection, string documentId, T document) where T : class =>
        await database.Collection(collection).Document(documentId).SetAsync(document);

    public async Task DeleteAsync(string collection, string documentId) =>
        await database.Collection(collection).Document(documentId).DeleteAsync();

    public Task<TResult> RunTransactionAsync<TResult>(Func<IReLoopDocumentTransaction, Task<TResult>> operation) =>
        database.RunTransactionAsync(transaction => operation(new FirestoreDocumentTransaction(database, transaction)));
}

internal sealed class FirestoreDocumentTransaction(FirestoreDb database, Transaction transaction) : IReLoopDocumentTransaction
{
    public async Task<T?> GetAsync<T>(string collection, string documentId) where T : class
    {
        var snapshot = await transaction.GetSnapshotAsync(database.Collection(collection).Document(documentId));
        return snapshot.Exists ? snapshot.ConvertTo<T>() : null;
    }

    public void Set<T>(string collection, string documentId, T document) where T : class =>
        transaction.Set(database.Collection(collection).Document(documentId), document);

    public void Delete(string collection, string documentId) =>
        transaction.Delete(database.Collection(collection).Document(documentId));
}
