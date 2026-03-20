using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Update.Internal;

namespace EF.TemporalExtensions;

/// <summary>
/// Extends the SQL Server migrations SQL generator to emit an additional
/// <c>ALTER TABLE … SET (SYSTEM_VERSIONING = ON (… HISTORY_RETENTION_PERIOD = …))</c>
/// statement whenever a temporal table is created or altered with a retention period
/// configured via <see cref="TemporalTableBuilderExtensions.HasRetentionPeriod"/>.
///
/// SQL Server does not support specifying <c>HISTORY_RETENTION_PERIOD</c> at table
/// creation time — it must be applied as a separate <c>ALTER TABLE</c> after
/// <c>SYSTEM_VERSIONING</c> is already <c>ON</c>.
/// </summary>
public sealed class TemporalRetentionMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
{
    // EF Core annotation names for SQL Server temporal tables.
    private const string IsTemporalAnnotation        = "SqlServer:IsTemporal";
    private const string HistoryTableNameAnnotation  = "SqlServer:TemporalHistoryTableName";
    private const string HistoryTableSchemaAnnotation = "SqlServer:TemporalHistoryTableSchema";

    /// <inheritdoc/>
    public TemporalRetentionMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        ICommandBatchPreparer commandBatchPreparer)
        : base(dependencies, commandBatchPreparer) { }

    /// <inheritdoc/>
    protected override void Generate(
        CreateTableOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        // Let EF Core generate the full temporal CREATE TABLE + ADD PERIOD + SET SYSTEM_VERSIONING.
        base.Generate(operation, model, builder, terminate);

        // Append the retention period ALTER TABLE if configured.
        AppendRetentionPeriodIfPresent(operation, operation.Schema, operation.Name, builder);
    }

    /// <inheritdoc/>
    protected override void Generate(
        AlterTableOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        base.Generate(operation, model, builder);

        // Only emit when the retention annotation has actually changed.
        var newValue = operation.FindAnnotation(TemporalRetentionAnnotation.Name)?.Value as string;
        var oldValue = operation.OldTable.FindAnnotation(TemporalRetentionAnnotation.Name)?.Value as string;

        if (newValue != oldValue)
            AppendRetentionPeriodIfPresent(operation, operation.Schema, operation.Name, builder);
    }

    // -------------------------------------------------------------------------

    private void AppendRetentionPeriodIfPresent(
        Annotatable operation,
        string? schema,
        string tableName,
        MigrationCommandListBuilder builder)
    {
        // Only act on temporal tables.
        if (operation[IsTemporalAnnotation] as bool? != true)
            return;

        var retentionValue = operation[TemporalRetentionAnnotation.Name] as string;
        if (string.IsNullOrEmpty(retentionValue))
            return;

        var retention = TemporalRetentionPeriod.Deserialize(retentionValue);

        var historyTableName   = operation[HistoryTableNameAnnotation]   as string ?? tableName + "History";
        var historyTableSchema = operation[HistoryTableSchemaAnnotation] as string;

        var table        = QualifiedName(schema, tableName);
        var historyTable = QualifiedName(historyTableSchema, historyTableName);

        builder
            .Append("ALTER TABLE ")
            .Append(table)
            .Append(" SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = ")
            .Append(historyTable)
            .Append(", HISTORY_RETENTION_PERIOD = ")
            .Append(retention.ToSqlFragment())
            .Append("))")
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);

        builder.EndCommand();
    }

    private static string QualifiedName(string? schema, string name)
    {
        var escapedName = $"[{name.Replace("]", "]]")}]";
        return schema is null
            ? escapedName
            : $"[{schema.Replace("]", "]]")}].{escapedName}";
    }
}
