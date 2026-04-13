CREATE TABLE [dbo].[SecurityLogs]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [OperationType] NVARCHAR(50) NOT NULL,
    [OperationModule] NVARCHAR(100) NOT NULL,
    [Content] NVARCHAR(2000) NOT NULL,
    [OperatorUserId] NVARCHAR(50) NULL,
    [OperatorName] NVARCHAR(100) NULL,
    [IPAddress] NVARCHAR(64) NULL,
    [CreatedAt] DATETIME NOT NULL CONSTRAINT [DF_SecurityLogs_CreatedAt] DEFAULT (GETDATE())
);
GO

CREATE NONCLUSTERED INDEX [IX_SecurityLogs_OperationType] ON [dbo].[SecurityLogs]([OperationType]);
GO
CREATE NONCLUSTERED INDEX [IX_SecurityLogs_OperationModule] ON [dbo].[SecurityLogs]([OperationModule]);
GO
CREATE NONCLUSTERED INDEX [IX_SecurityLogs_OperatorName] ON [dbo].[SecurityLogs]([OperatorName]);
GO
CREATE NONCLUSTERED INDEX [IX_SecurityLogs_CreatedAt] ON [dbo].[SecurityLogs]([CreatedAt] DESC);
GO
