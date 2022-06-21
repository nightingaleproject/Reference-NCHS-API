﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using messaging.Models;

#nullable disable

namespace messaging.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20220601204108_AddSteveRetrievedAt")]
    partial class AddSteveRetrievedAt
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("messaging.Models.IJEItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("IJE")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MessageId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("IJEItems");
                });

            modelBuilder.Entity("messaging.Models.IncomingMessageItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("CertificateNumber")
                        .HasMaxLength(6)
                        .HasColumnType("CHAR(6)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("EventType")
                        .HasMaxLength(3)
                        .HasColumnType("CHAR(3)");

                    b.Property<long?>("EventYear")
                        .HasColumnType("bigint");

                    b.Property<string>("JurisdictionId")
                        .IsRequired()
                        .HasMaxLength(2)
                        .HasColumnType("CHAR(2)");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MessageId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MessageType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProcessedStatus")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("CHAR(10)");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("CHAR(3)");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("IncomingMessageItems");
                });

            modelBuilder.Entity("messaging.Models.IncomingMessageLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("JurisdictionId")
                        .IsRequired()
                        .HasMaxLength(2)
                        .HasColumnType("CHAR(2)");

                    b.Property<string>("MessageId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTimeOffset?>("MessageTimestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NCHSIdentifier")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("StateAuxiliaryIdentifier")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("MessageId");

                    b.HasIndex("NCHSIdentifier");

                    b.ToTable("IncomingMessageLogs");
                });

            modelBuilder.Entity("messaging.Models.OutgoingMessageItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("CertificateNumber")
                        .HasMaxLength(6)
                        .HasColumnType("CHAR(6)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("EventType")
                        .HasMaxLength(3)
                        .HasColumnType("CHAR(3)");

                    b.Property<long?>("EventYear")
                        .HasColumnType("bigint");

                    b.Property<string>("JurisdictionId")
                        .IsRequired()
                        .HasMaxLength(2)
                        .HasColumnType("CHAR(2)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MessageId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MessageType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("RetrievedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("SteveRetrievedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("CreatedDate");

                    b.ToTable("OutgoingMessageItems");
                });
#pragma warning restore 612, 618
        }
    }
}
