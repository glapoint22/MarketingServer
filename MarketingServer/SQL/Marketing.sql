CREATE TABLE Niches(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	Name varchar(20) NOT NULL,
	LeadPage varchar(255)
);

CREATE TABLE Campaigns(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	Name varchar(20) NOT NULL,
	NicheID int NOT NULL,
	FOREIGN KEY (NicheID) REFERENCES Niches(ID)
);

CREATE TABLE Customers(
	Email varchar(50) NOT NULL,
	NicheID int NOT NULL,
	Name varchar(50) NOT NULL,
	CurrentCampaignID int NOT NULL,
	CurrentCampaignDay int NOT NULL,
	Active bit DEFAULT 1 NOT NULL,
	Subscribed bit DEFAULT 1 NOT NULL,
	DateSubscribed datetime DEFAULT GETDATE() NOT NULL,
	DateUnsubscribed datetime,
	EmailSentDate datetime NOT NULL,
	EmailSendFrequency int default 1 NOT NULL,
);

ALTER TABLE Customers DROP constraint PK__Customer__3214EC2701B7CE82
ALTER TABLE Customers DROP COLUMN id
alter table CustomerCampaigns add primary key(CustomerID, NicheID)
alter table CustomerCampaigns alter column customerID uniqueidentifier not null
alter table customercampaigns add FOREIGN KEY (CustomerID) REFERENCES Customers(Email)
alter table Customers add primary key(Email)

CREATE TABLE Subscriptions(
	CustomerID varchar(50) NOT NULL,
	NicheID int NOT NULL,
	CurrentCampaignID int NOT NULL,
	CurrentEmailDay int NOT NULL,
	Active bit NOT NULL,
	Subscribed bit NOT NULL,
	DateSubscribed datetime NOT NULL,
	DateUnsubscribed datetime,
	PRIMARY KEY(CustomerID, NicheID),
	FOREIGN KEY (CustomerID) REFERENCES Customers(ID) ON DELETE CASCADE ON UPDATE CASCADE,
	FOREIGN KEY (NicheID) REFERENCES Niches(ID),
	FOREIGN KEY (CurrentCampaignID) REFERENCES Campaigns(ID)
);




alter table Customers add Unsubscribed bit

alter table customers add EmailFrequency int default 1 not null

alter table CustomerCampaigns alter column CustomerID varchar(50) not null

CREATE TABLE Emails(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	Subject varchar(50) NOT NULL,
	Body varchar(MAX) NOT NULL,
	CampaignID int NOT NULL,
	Day int NOT NULL,
	FOREIGN KEY (CampaignID) REFERENCES Campaigns(ID)
);

Create NonClustered Index Customers_EmailSendDate on Customers(EmailSendDate)

Create NonClustered Index IX_Active_Subscribed on CustomerCampaigns(Active, Subscribed)




SELECT Name, Email, CampaignID, CurrentCampaignDay
FROM Customers
WHERE EmailSendDate = DATEADD(day, DATEDIFF(day, 0, GETDATE()), 0)

SELECT Subject, Body
FROM Emails
WHERE CampaignID = 13 AND Day = 2





 
select * from campaigns
select * from niches
select * from customers
select * from Subscriptions
select * from emails where id = '0858A9A9-8DE4-4201-864E-5B18162FD3D8'

Create NonClustered Index IX_CampainID_Day on emails(campaignID, day)

alter table niches alter column emailid uniqueidentifier not null

delete Subscriptions
alter table niches add FOREIGN KEY (emailid) REFERENCES emails(ID)
alter table subscriptions drop column currentemailday
alter table subscriptions alter column customerid uniqueidentifier not null

alter table niches add EmailID uniqueidentifier

alter table subscriptions add NextEmailToSend uniqueidentifier not null

ALTER TABLE subscriptions ADD CONSTRAINT UQ_Email UNIQUE (Email)

select ID from campaigns where nicheID = 1 and ID > 1

insert into emails values(16, 4, 'Gaming Campaign 4 day 4', 'Gaming Campaign 4 day 4')

select count(customerid) from subscriptions where nicheid = 4 and customerid = 'asdf@asdf'

alter table emails drop column id
alter table emails add primary key(ID)

alter table customers add primary key(id)

alter table customers drop column email
alter table customers drop constraint PK__Customer__A9D1053581025809
alter table Customers add Email varchar(50) not null

delete customers



delete CustomerCampaigns where customerid in (select id from Customers where Email = 'sam@dasf.com')

alter table CustomerCampaigns add CustomerID varchar(50)
ALTER TABLE CustomerCampaigns DROP constraint [FK__CustomerC__Custo__1DB06A4F]

ALTER TABLE CustomerCampaigns
   ADD CONSTRAINT FK_CustomerCampaigns_CustomerID
   FOREIGN KEY (CustomerID) REFERENCES Customers(ID) ON DELETE CASCADE

   select day from emails where id = '69FDADB1-FBC0-450B-81E7-00F1620074D0'

   select id from emails where campaignid = 6 and day > 2

   select id from emails where campaignid = 6 and day > 2

   select id from emails, (select campaignid, day from emails where id = '69FDADB1-FBC0-450B-81E7-00F1620074D0') as a 
   where emails.CampaignID = a.CampaignID and emails.Day > a.Day
   