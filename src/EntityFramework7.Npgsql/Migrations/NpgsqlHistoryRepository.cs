﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using EntityFramework7.Npgsql;
using EntityFramework7.Npgsql.Metadata;
using EntityFramework7.Npgsql.Migrations;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Migrations.History;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Update;
using Microsoft.Data.Entity.Utilities;

namespace EntityFramework7.Npgsql.Migrations
{
    public class NpgsqlHistoryRepository : HistoryRepository
    {
        private readonly NpgsqlUpdateSqlGenerator _sql;

        public NpgsqlHistoryRepository(
            [NotNull] IDatabaseCreator databaseCreator,
            [NotNull] ISqlStatementExecutor executor,
            [NotNull] NpgsqlDatabaseConnection connection,
            [NotNull] IMigrationModelFactory modelFactory,
            [NotNull] IDbContextOptions options,
            [NotNull] IModelDiffer modelDiffer,
            [NotNull] NpgsqlMigrationSqlGenerator migrationSqlGenerator,
            [NotNull] NpgsqlMetadataExtensionProvider annotations,
            [NotNull] NpgsqlUpdateSqlGenerator updateSqlGenerator,
            [NotNull] IServiceProvider serviceProvider)
            : base(
                  databaseCreator,
                  executor,
                  connection,
                  modelFactory,
                  options,
                  modelDiffer,
                  migrationSqlGenerator,
                  annotations,
                  updateSqlGenerator,
                  serviceProvider)
        {
            Check.NotNull(updateSqlGenerator, nameof(updateSqlGenerator));

            _sql = updateSqlGenerator;
        }

        protected override string ExistsSql
        {
            get
            {
                var builder = new StringBuilder();

                builder.Append("SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE ");

                if (TableSchema != null)
                {
                    builder
                        .Append("n.nspname='")
                        .Append(_sql.EscapeLiteral(TableSchema))
                        .Append("' AND ");
                }

                builder
                    .Append("c.relname='")
                    .Append(_sql.EscapeLiteral(TableName))
                    .Append("';");

                return builder.ToString();
            }
        }

        protected override bool Exists(object value) => value != DBNull.Value;

        public override string GetInsertScript(HistoryRow row)
        {
            Check.NotNull(row, nameof(row));

            return new StringBuilder().Append("INSERT INTO ")
                .Append(_sql.DelimitIdentifier(TableName, TableSchema))
                .Append(" (")
                .Append(_sql.DelimitIdentifier(MigrationIdColumnName))
                .Append(", ")
                .Append(_sql.DelimitIdentifier(ProductVersionColumnName))
                .AppendLine(")")
                .Append("VALUES ('")
                .Append(_sql.EscapeLiteral(row.MigrationId))
                .Append("', '")
                .Append(_sql.EscapeLiteral(row.ProductVersion))
                .Append("');")
                .ToString();
        }

        public override string GetDeleteScript(string migrationId)
        {
            Check.NotEmpty(migrationId, nameof(migrationId));

            return new StringBuilder().Append("DELETE FROM ")
                .AppendLine(_sql.DelimitIdentifier(TableName, TableSchema))
                .Append("WHERE ")
                .Append(_sql.DelimitIdentifier(MigrationIdColumnName))
                .Append(" = '")
                .Append(_sql.EscapeLiteral(migrationId))
                .Append("';")
                .ToString();
        }

        public override string GetCreateIfNotExistsScript()
        {
            var builder = new IndentedStringBuilder();

            builder.Append("IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE ");

            if (TableSchema != null)
            {
                builder
                    .Append("n.nspname='")
                    .Append(_sql.EscapeLiteral(TableSchema))
                    .Append("' AND ");
            }

            builder
                .Append("c.relname='")
                .Append(_sql.EscapeLiteral(TableName))
                .Append("') THEN");


            builder.AppendLines(GetCreateScript());

            builder.Append(GetEndIfScript());

            return builder.ToString();
        }

        public override string GetBeginIfNotExistsScript(string migrationId)
        {
            Check.NotEmpty(migrationId, nameof(migrationId));

            return new StringBuilder()
                .Append("IF NOT EXISTS(SELECT * FROM ")
                .Append(_sql.DelimitIdentifier(TableName, TableSchema))
                .Append(" WHERE ")
                .Append(_sql.DelimitIdentifier(MigrationIdColumnName))
                .Append(" = '")
                .Append(_sql.EscapeLiteral(migrationId))
                .AppendLine("')")
                .Append("BEGIN")
                .ToString();
        }

        public override string GetBeginIfExistsScript(string migrationId)
        {
            Check.NotEmpty(migrationId, nameof(migrationId));

            return new StringBuilder()
                .Append("IF EXISTS(SELECT * FROM ")
                .Append(_sql.DelimitIdentifier(TableName, TableSchema))
                .Append(" WHERE ")
                .Append(_sql.DelimitIdentifier(MigrationIdColumnName))
                .Append(" = '")
                .Append(_sql.EscapeLiteral(migrationId))
                .AppendLine("')")
                .Append("BEGIN")
                .ToString();
        }

        public override string GetEndIfScript() => "END IF";
    }
}
