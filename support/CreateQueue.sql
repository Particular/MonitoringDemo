SET QUOTED_IDENTIFIER ON

if not  exists (select * from sys.objects where object_id = object_id(N'[dbo].[{arg}]') and type in (N'U'))
        begin
        create table [dbo].[{arg}](
            [Id] [uniqueidentifier] not null,
            [CorrelationId] [varchar](255),
            [ReplyToAddress] [varchar](255),
            [Recoverable] [bit] not null,
            [Expires] [datetime],
            [Headers] [nvarchar](max) not null,
            [Body] [varbinary](max),
            [RowVersion] [bigint] identity(1,1) not null
        );
        create clustered index [Index_RowVersion] on [dbo].[{arg}]
        (
            [RowVersion]
        )
        create nonclustered index [Index_Expires] on [dbo].[{arg}]
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