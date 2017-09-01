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
	ID uniqueidentifier default newid() PRIMARY KEY,
	Name varchar(50) NOT NULL,
	Email varchar(50) NOT NULL,
	EmailSendDate datetime NOT NULL,
	EmailFrequency int default 1 NOT NULL,
);

ALTER TABLE Customers DROP constraint PK__Customer__3214EC2701B7CE82
ALTER TABLE Customers DROP COLUMN id
alter table CustomerCampaigns add primary key(CustomerID, NicheID)
alter table CustomerCampaigns alter column customerID uniqueidentifier not null
alter table customercampaigns add FOREIGN KEY (CustomerID) REFERENCES Customers(ID)

CREATE TABLE CustomerCampaigns(
	CustomerID uniqueidentifier NOT NULL,
	NicheID int NOT NULL,
	Active bit NOT NULL,
	CurrentCampaignID int NOT NULL,
	CurrentCampaignDay int NOT NULL,
	Subscribed bit DEFAULT 1 NOT NULL,
	DateSubscribed datetime DEFAULT GETDATE() NOT NULL,
	DateUnsubscribed datetime,
	PRIMARY KEY(CustomerID, NicheID),
	FOREIGN KEY (CustomerID) REFERENCES Customers(ID),
	FOREIGN KEY (NicheID) REFERENCES Niches(ID),
	FOREIGN KEY (CurrentCampaignID) REFERENCES Campaigns(ID)
);

alter table CustomerCampaigns add CustomerID uniqueidentifier
alter table Customers add Unsubscribed bit

alter table customers add EmailFrequency int default 1 not null

alter table Customers alter column Unsubscribed bit not null

CREATE TABLE Emails(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	Subject varchar(50) NOT NULL,
	Body varchar(MAX) NOT NULL,
	CampaignID int NOT NULL,
	Day int NOT NULL,
	FOREIGN KEY (CampaignID) REFERENCES Campaigns(ID)
);

Create NonClustered Index Customers_EmailSendDate on Customers(EmailSendDate)

Create NonClustered Index Emails_CampaignID_Day on Emails(CampaignID, Day)




SELECT Name, Email, CampaignID, CurrentCampaignDay
FROM Customers
WHERE EmailSendDate = DATEADD(day, DATEDIFF(day, 0, GETDATE()), 0)

SELECT Subject, Body
FROM Emails
WHERE CampaignID = 13 AND Day = 2





 
select * from campaigns
select * from customers
select * from niches
select * from CustomerCampaigns
select * from emails

select ID from campaigns where nicheID = 1 and ID > 1

insert into emails values(16, 4, 'Gaming Campaign 4 day 4', 'Gaming Campaign 4 day 4')



alter table emails drop column id
alter table emails add primary key(campaignID, Day)