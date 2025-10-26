IF EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'MNT_Task_Execution_Tablet'))
BEGIN
    -- La tabla existe
    PRINT 'La tabla existe';
END
ELSE
BEGIN
    -- La tabla no existe
    PRINT 'La tabla no existe, la creamos';
	
	CREATE TABLE MNT_Task_Execution_Tablet (
		UnitCode VARCHAR(10) NOT NULL,
		TaskCode VARCHAR(10) NOT NULL,
		FunCode VARCHAR(30) NOT NULL,
		[Date] DATETIME NOT NULL,
		TaskExecutionStatusCode VARCHAR(10) NOT NULL,
		Imagen VARBINARY(MAX) NULL,
		trz_cUserCode bigint DEFAULT NULL,
		trz_cDate datetime DEFAULT NULL,
		trz_mUserCode Bigint DEFAULT NULL,
		trz_mDate datetime DEFAULT NULL
	);
END
GO