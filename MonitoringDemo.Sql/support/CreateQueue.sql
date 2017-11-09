SET QUOTED_IDENTIFIER ON
GO

if not  exists (select * from sys.objects where object_id = object_id(N'[dbo].[$(queueName)]') and type in (N'U'))
        begin
        create table [dbo].[$(queueName)](
            [Id] [uniqueidentifier] not null,
            [CorrelationId] [varchar](255),
            [ReplyToAddress] [varchar](255),
            [Recoverable] [bit] not null,
            [Expires] [datetime],
            [Headers] [nvarchar](max) not null,
            [Body] [varbinary](max),
            [RowVersion] [bigint] identity(1,1) not null
        );
        create clustered index [Index_RowVersion] on [dbo].[$(queueName)]
        (
            [RowVersion]
        )
        create nonclustered index [Index_Expires] on [dbo].[$(queueName)]
        (
            [Expires]
        )
        include
        (
            [Id],
            [RowVersion]
        )
        where
            [Expires] is not null
    end