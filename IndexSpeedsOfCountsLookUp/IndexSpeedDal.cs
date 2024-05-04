using Dapper;
using System.Data.SqlClient;

namespace IndexSpeedsOfCountsLookUp;

public class IndexSpeedDal
{
    private const string connectionString = "Server=.;Database=TestIndicies;Trusted_Connection=True;MultipleActiveResultSets=true;";

    public async Task<TextCount> SaveDataUpdateText(string text, int cacheTimeInSeconds)
    {
        string sql = $@"
            DECLARE @HashedText [varbinary](16) = (SELECT HashBytes('MD5', @Text))
            
            MERGE [dbo].[UpdateTextCountRows] AS LF
            USING (SELECT @HashedText AS [Text], @Partition AS [Partition], @UtcNow AS [UtcNow]) AS RT
                ON LF.[Text] = RT.[Text] AND LF.[Partition] = RT.[Partition]
            WHEN NOT MATCHED
                THEN
                    INSERT ([Partition]
                            ,[Text]
                            ,[Count]
                            ,[CountStartedOn])
                    VALUES (RT.[Partition]
                            ,RT.[Text]
                            ,1
                            ,RT.[UtcNow])
            WHEN MATCHED
                THEN
                    UPDATE
                    SET LF.[Count] = (CASE
                                            WHEN DATEDIFF(ss, LF.[CountStartedOn], RT.[UtcNow]) < @LookBackTimeInSeconds THEN LF.[Count] + 1
                                            ELSE 1
                                      END),
                        LF.[CountStartedOn] = (CASE
                                                    WHEN DATEDIFF(ss, LF.[CountStartedOn], RT.[UtcNow]) < @LookBackTimeInSeconds THEN LF.[CountStartedOn]
                                                    ELSE RT.[UtcNow]
                                              END)
            OUTPUT INSERTED.Count;
            ";
        
        using SqlConnection con = new SqlConnection(connectionString);
        int count = await con.ExecuteScalarAsync<int>(sql,
            new { Text = text, Partition = 1, DateTime.UtcNow, LookBackTimeInSeconds = cacheTimeInSeconds });
        return new TextCount(text, count);
    }

    public async Task<TextCount> SaveDataInsertNewText(string text, int cacheTimeInSeconds)
    {
        string sql = $@"
            DECLARE @HashedText [varbinary](16) = (SELECT HashBytes('MD5', @Text))
            
            MERGE [dbo].[InsertNewTextCountRows] AS LF
            USING (SELECT @HashedText AS [Text], @Partition AS [Partition], @UtcNow AS [UtcNow]) AS RT
                ON LF.[Text] = RT.[Text] AND LF.[Partition] = RT.[Partition] AND DATEDIFF(ss, LF.[CountStartedOn], RT.[UtcNow]) < @LookBackTimeInSeconds
            WHEN NOT MATCHED
                THEN
                    INSERT ([Partition]
                            ,[Text]
                            ,[Count]
                            ,[CountStartedOn])
                    VALUES (RT.[Partition]
                            ,RT.[Text]
                            ,1
                            ,RT.[UtcNow])
            WHEN MATCHED
                THEN
                    UPDATE
                    SET LF.[Count] = LF.[Count] + 1
            OUTPUT INSERTED.Count;
            ";

        using SqlConnection con = new SqlConnection(connectionString);
        int count = await con.ExecuteScalarAsync<int>(sql,
            new { Text = text, Partition = 1, DateTime.UtcNow, LookBackTimeInSeconds = cacheTimeInSeconds });
        return new TextCount(text, count);
    }
}
