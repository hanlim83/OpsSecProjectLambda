﻿using Microsoft.EntityFrameworkCore;
using OpsSecProjectLambda.Abstractions;
using System;

namespace OpsSecProjectLambda.EF
{
    public class LogContext : DbContext
    {
        public LogContext(DbContextOptions<LogContext> options) : base(options)
        {
        }
        public DbSet<LogInput> LogInputs { get; set; }
        public DbSet<S3Bucket> S3Buckets { get; set; }
        public DbSet<GlueDatabase> GlueDatabases { get; set; }
        public DbSet<GlueDatabaseTable> GlueDatabaseTables { get; set; }
        public DbSet<GlueConsolidatedEntity> GlueConsolidatedEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LogInput>().ToTable("LogInputs");
            modelBuilder.Entity<LogInput>().Property(l => l.InitialIngest).HasDefaultValue(false);
            modelBuilder.Entity<LogInput>().HasAlternateKey(l => l.Name).HasName("AlternateKey_LogInputName");
            modelBuilder.Entity<S3Bucket>().ToTable("S3Buckets");
            modelBuilder.Entity<S3Bucket>().HasAlternateKey(b => b.Name).HasName("AlternateKey_BucketName");
            modelBuilder.Entity<GlueDatabase>().ToTable("GlueDatabases");
            modelBuilder.Entity<GlueDatabaseTable>().ToTable("GlueDatabaseTables");
            modelBuilder.Entity<GlueConsolidatedEntity>().ToTable("GlueConsolidatedEntities");
        }
    }
}
