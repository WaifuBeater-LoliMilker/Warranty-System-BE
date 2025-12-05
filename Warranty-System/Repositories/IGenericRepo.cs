using System.Linq.Expressions;
namespace Warranty_System.Repositories;
public interface IGenericRepo
{
    Task<List<T>> GetAll<T>() where T : class;

    Task<T?> GetById<T>(int id) where T : class;

    Task<T> Insert<T>(T entity) where T : class;

    Task<T> Update<T>(T entity) where T : class;

    Task<bool> Delete<T>(T entity) where T : class;

    Task<bool> DeleteById<T>(int id) where T : class;

    Task<bool> DeleteByExpression<T>(Expression<Func<T, bool>> predicate) where T : class;

    Task<List<T>> FindByExpression<T>(Expression<Func<T, bool>> predicate) where T : class;

    Task<T?> FindModel<T>(Expression<Func<T, bool>> predicate) where T : class;

    Task<List<T>> ProcedureToList<T>(string procedureName, string[] parameterName, object[] parameterValue) where T : class;

    Task<Tuple<List<T1>, List<T2>>> ProcedureToList<T1, T2>(string procedureName, string[] parameterName, object[] parameterValue) where T1 : class where T2 : class;

    Task<Tuple<List<T1>, List<T2>, List<T3>>> ProcedureToList<T1, T2, T3>(string procedureName, string[] parameterName, object[] parameterValue) where T1 : class where T2 : class where T3 : class;

    Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ProcedureToList<T1, T2, T3, T4>(string procedureName, string[] parameterName, object[] parameterValue) where T1 : class where T2 : class where T3 : class where T4 : class;

    Task<int> ExecuteProcedureAsync(string procedureName, string[] parameterName, object[] parameterValue);
}