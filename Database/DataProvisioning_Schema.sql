IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [reports] (
        [id] int NOT NULL IDENTITY,
        [name] nvarchar(max) NOT NULL,
        [url] nvarchar(max) NULL,
        [description] nvarchar(max) NULL,
        CONSTRAINT [PK_reports] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [users] (
        [id] int NOT NULL IDENTITY,
        [name] nvarchar(max) NOT NULL,
        [email] nvarchar(max) NOT NULL,
        [role] nvarchar(max) NOT NULL,
        [avatar] nvarchar(max) NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_users] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [virtual_groups] (
        [id] int NOT NULL IDENTITY,
        [name] nvarchar(max) NOT NULL,
        [owner_id] int NOT NULL,
        [description] nvarchar(max) NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_virtual_groups] PRIMARY KEY ([id]),
        CONSTRAINT [FK_virtual_groups_users_owner_id] FOREIGN KEY ([owner_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [datasets] (
        [id] int NOT NULL IDENTITY,
        [name] nvarchar(max) NOT NULL,
        [type] nvarchar(max) NOT NULL,
        [description] nvarchar(max) NULL,
        [owner_group_id] int NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_datasets] PRIMARY KEY ([id]),
        CONSTRAINT [FK_datasets_virtual_groups_owner_group_id] FOREIGN KEY ([owner_group_id]) REFERENCES [virtual_groups] ([id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [virtual_group_members] (
        [group_id] int NOT NULL,
        [user_id] int NOT NULL,
        [added_at] datetime2 NOT NULL,
        CONSTRAINT [PK_virtual_group_members] PRIMARY KEY ([group_id], [user_id]),
        CONSTRAINT [FK_virtual_group_members_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_virtual_group_members_virtual_groups_group_id] FOREIGN KEY ([group_id]) REFERENCES [virtual_groups] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [asset_policy_groups] (
        [id] int NOT NULL IDENTITY,
        [dataset_id] int NOT NULL,
        [owner_id] int NULL,
        [name] nvarchar(max) NOT NULL,
        [description] nvarchar(max) NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_asset_policy_groups] PRIMARY KEY ([id]),
        CONSTRAINT [FK_asset_policy_groups_datasets_dataset_id] FOREIGN KEY ([dataset_id]) REFERENCES [datasets] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_asset_policy_groups_users_owner_id] FOREIGN KEY ([owner_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [columns] (
        [id] int NOT NULL IDENTITY,
        [dataset_id] int NOT NULL,
        [name] nvarchar(max) NOT NULL,
        [data_type] nvarchar(max) NULL,
        [definition] nvarchar(max) NULL,
        [is_pii] bit NOT NULL,
        [sample_data] nvarchar(max) NULL,
        CONSTRAINT [PK_columns] PRIMARY KEY ([id]),
        CONSTRAINT [FK_columns_datasets_dataset_id] FOREIGN KEY ([dataset_id]) REFERENCES [datasets] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [report_datasets] (
        [dataset_id] int NOT NULL,
        [report_id] int NOT NULL,
        CONSTRAINT [PK_report_datasets] PRIMARY KEY ([dataset_id], [report_id]),
        CONSTRAINT [FK_report_datasets_datasets_dataset_id] FOREIGN KEY ([dataset_id]) REFERENCES [datasets] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_report_datasets_reports_report_id] FOREIGN KEY ([report_id]) REFERENCES [reports] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [access_requests] (
        [id] int NOT NULL IDENTITY,
        [user_id] int NOT NULL,
        [dataset_id] int NOT NULL,
        [status] nvarchar(max) NOT NULL,
        [requested_rls_filters] nvarchar(max) NULL,
        [justification] nvarchar(max) NULL,
        [reviewed_by] int NULL,
        [reviewed_at] datetime2 NULL,
        [created_at] datetime2 NOT NULL,
        [policy_group_id] int NULL,
        CONSTRAINT [PK_access_requests] PRIMARY KEY ([id]),
        CONSTRAINT [FK_access_requests_asset_policy_groups_policy_group_id] FOREIGN KEY ([policy_group_id]) REFERENCES [asset_policy_groups] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_access_requests_datasets_dataset_id] FOREIGN KEY ([dataset_id]) REFERENCES [datasets] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_access_requests_users_reviewed_by] FOREIGN KEY ([reviewed_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_access_requests_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [asset_policy_columns] (
        [id] int NOT NULL IDENTITY,
        [policy_group_id] int NOT NULL,
        [column_name] nvarchar(max) NOT NULL,
        [is_hidden] bit NOT NULL,
        CONSTRAINT [PK_asset_policy_columns] PRIMARY KEY ([id]),
        CONSTRAINT [FK_asset_policy_columns_asset_policy_groups_policy_group_id] FOREIGN KEY ([policy_group_id]) REFERENCES [asset_policy_groups] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE TABLE [asset_policy_conditions] (
        [id] int NOT NULL IDENTITY,
        [policy_group_id] int NOT NULL,
        [column_name] nvarchar(max) NOT NULL,
        [operator] nvarchar(max) NOT NULL,
        [value] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_asset_policy_conditions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_asset_policy_conditions_asset_policy_groups_policy_group_id] FOREIGN KEY ([policy_group_id]) REFERENCES [asset_policy_groups] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_access_requests_dataset_id] ON [access_requests] ([dataset_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_access_requests_policy_group_id] ON [access_requests] ([policy_group_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_access_requests_reviewed_by] ON [access_requests] ([reviewed_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_access_requests_user_id] ON [access_requests] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_asset_policy_columns_policy_group_id] ON [asset_policy_columns] ([policy_group_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_asset_policy_conditions_policy_group_id] ON [asset_policy_conditions] ([policy_group_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_asset_policy_groups_dataset_id] ON [asset_policy_groups] ([dataset_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_asset_policy_groups_owner_id] ON [asset_policy_groups] ([owner_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_columns_dataset_id] ON [columns] ([dataset_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_datasets_owner_group_id] ON [datasets] ([owner_group_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_report_datasets_report_id] ON [report_datasets] ([report_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_virtual_group_members_user_id] ON [virtual_group_members] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_virtual_groups_owner_id] ON [virtual_groups] ([owner_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260324170905_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260324170905_InitialCreate', N'10.0.5');
END;

COMMIT;
GO

