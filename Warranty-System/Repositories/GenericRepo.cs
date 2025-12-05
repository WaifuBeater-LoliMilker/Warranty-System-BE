using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using Warranty_System.Models.Context;

namespace Warranty_System.Repositories;

public class GenericRepo : IGenericRepo
{
    private readonly warranty_systemContext _context;
    private readonly IDbConnection _dbConnection;

    public GenericRepo(warranty_systemContext context, IDbConnection dbConnection)
    {
        _context = context;
        _dbConnection = dbConnection;
    }

    // Lấy tất cả thực thể (không theo dõi)
    public async Task<List<T>> GetAll<T>() where T : class
    {
        return await _context.Set<T>().AsNoTracking().ToListAsync();
    }

    // Tìm thực thể theo ID (không theo dõi)
    public async Task<T?> GetById<T>(int id) where T : class
    {
        var entity = await _context.Set<T>().FindAsync(id);
        if (entity != null)
        {
            _context.Entry(entity).State = EntityState.Detached; // Ngắt theo dõi sau khi tìm
        }
        return entity;
    }

    // Thêm thực thể mới (không theo dõi sau khi thêm)
    public async Task<T> Insert<T>(T entity) where T : class
    {
        _context.Entry(entity).State = EntityState.Added; // Đánh dấu trạng thái là Added
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached; // Ngắt theo dõi thực thể
        return entity;
    }

    // Cập nhật thực thể (không theo dõi sau khi cập nhật)
    public async Task<T> Update<T>(T entity) where T : class
    {
        _context.Entry(entity).State = EntityState.Modified; // Đánh dấu trạng thái là Modified
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached; // Ngắt theo dõi thực thể
        return entity;
    }

    public async Task<bool> Delete<T>(T entity) where T : class
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    // Xóa thực thể theo ID
    public async Task<bool> DeleteById<T>(int id) where T : class
    {
        var entity = await GetById<T>(id);
        if (entity == null)
        {
            return false; // Không tìm thấy thực thể
        }

        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByExpression<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
            var dbSet = _context.Set<T>();
            // Lấy danh sách các thực thể thỏa mãn điều kiện (predicate)
            var entities = dbSet.Where(predicate);
            // Kiểm tra nếu không có thực thể nào
            if (!entities.Any())
                return false;

            dbSet.RemoveRange(entities);

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Tìm thực thể theo điều kiện (không theo dõi)
    public async Task<List<T>> FindByExpression<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return await _context.Set<T>()
            .AsNoTracking() // Không theo dõi
            .Where(predicate)
            .ToListAsync();
    }

    public async Task<T?> FindModel<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        var entity = await _context.Set<T>()
                .AsNoTracking() // Không theo dõi
                .FirstOrDefaultAsync(predicate);
        return entity;
    }

    // Gọi stored procedure và nhận kết quả (SELECT)
    public async Task<List<T>> ProcedureToList<T>(string procedureName, string[] parameterNames, object[] parameterValues) where T : class
    {
        var parameters = GetSqlParameters(parameterNames, parameterValues);
        IEnumerable<T> result = await _dbConnection.QueryAsync<T>(procedureName, parameters, commandType: CommandType.StoredProcedure);
        return [.. result];
    }

    // TWO‐result:
    public async Task<Tuple<List<T1>, List<T2>>> ProcedureToList<T1, T2>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues
    ) where T1 : class where T2 : class
    {
        var parameters = GetSqlParameters(parameterNames, parameterValues);
        using var multi = await _dbConnection
            .QueryMultipleAsync(procedureName, parameters, commandType: CommandType.StoredProcedure);

        var list1 = multi.Read<T1>().ToList();
        var list2 = multi.Read<T2>().ToList();
        return Tuple.Create(list1, list2);
    }

    // THREE‐result:
    public async Task<Tuple<List<T1>, List<T2>, List<T3>>> ProcedureToList<T1, T2, T3>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues
    ) where T1 : class where T2 : class where T3 : class
    {
        var parameters = GetSqlParameters(parameterNames, parameterValues);
        using var multi = await _dbConnection
            .QueryMultipleAsync(procedureName, parameters, commandType: CommandType.StoredProcedure);

        var list1 = multi.Read<T1>().ToList();
        var list2 = multi.Read<T2>().ToList();
        var list3 = multi.Read<T3>().ToList();
        return Tuple.Create(list1, list2, list3);
    }

    // FOUR‐result:
    public async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>>> ProcedureToList<T1, T2, T3, T4>(
        string procedureName,
        string[] parameterNames,
        object[] parameterValues
    ) where T1 : class where T2 : class where T3 : class where T4 : class
    {
        var parameters = GetSqlParameters(parameterNames, parameterValues);
        using var multi = await _dbConnection
            .QueryMultipleAsync(procedureName, parameters, commandType: CommandType.StoredProcedure);

        var list1 = multi.Read<T1>().ToList();
        var list2 = multi.Read<T2>().ToList();
        var list3 = multi.Read<T3>().ToList();
        var list4 = multi.Read<T4>().ToList();
        return Tuple.Create(list1, list2, list3, list4);
    }

    public async Task<int> ExecuteProcedureAsync(string procedureName, string[] parameterNames, object[] parameterValues)
    {
        var parameters = GetSqlParameters(parameterNames, parameterValues);

        return await _dbConnection.ExecuteAsync(procedureName, parameters, commandType: CommandType.StoredProcedure);
    }

    // Phương thức chuyển đổi tên tham số và giá trị thành danh sách các MySqlParameter
    private DynamicParameters GetSqlParameters(string[] parameterNames, object[] parameterValues)
    {
        if (parameterNames.Length != parameterValues.Length)
        {
            throw new ArgumentException("The number of parameter names and values must be the same.");
        }

        // Create a DynamicParameters object for Dapper
        var parameters = new DynamicParameters();
        for (int i = 0; i < parameterNames.Length; i++)
        {
            parameters.Add(parameterNames[i], parameterValues[i]);
        }

        return parameters;
    }
}