IF EXISTS(SELECT * FROM sys.databases WHERE NAME = 'ParticularMonitoringDemo')
BEGIN
  DROP DATABASE ParticularMonitoringDemo
END
CREATE DATABASE ParticularMonitoringDemo
ON (NAME = 'Particular_dat', FILENAME = '{arg}\transport\ParticularMonitoringDemo.mdf')