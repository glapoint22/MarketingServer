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


CREATE TABLE Categories(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	Name varchar(255) not null
);


Create NonClustered Index Customers_EmailSendDate on Customers(EmailSendDate)

Create NonClustered Index IX_Active_Subscribed on CustomerCampaigns(Active, Subscribed)




SELECT Name, Email, CampaignID, CurrentCampaignDay
FROM Customers
WHERE EmailSendDate = DATEADD(day, DATEDIFF(day, 0, GETDATE()), 0)

SELECT Subject, Body
FROM Emails
WHERE CampaignID = 13 AND Day = 2


alter table Categories alter column Icon varchar(255) not null



CREATE TABLE RefreshTokens (
	ID nvarchar(255) not null primary key,
	ClientID varchar(255) not null,
	Ticket nvarchar(max) not null,
	Expires Datetime not null
);
 

select * from Categories
select * from Niches
select * from emailCampaigns order by ProductID,day
select * from customers
select * from Subscriptions
select * from CampaignRecords order by subscriptionid, date desc
select * from Products order by nicheid
select * from LeadPages
select * from LeadMagnetEmails
select * from ProductVideos
select * from CategoryImages
select * from ProductBanners
select * from Filters
select * from FilterLabels
select * from ProductFilters
select * from priceRange
select * from Clients
select * from RefreshTokens



alter table niches drop column leadpagebody
drop table vendors


update products set VendorID = null


select count(id) from products where name like '%diet%' and id in (select productID from ProductFilters where FilterLabelID in(25, 26, 27))

select * from ProductFilters where FilterLabelID = 27

delete ProductFilters where FilterLabelID = 27 and ProductID != '9151B3E80C'

CREATE TABLE Groups(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	Name varchar(255) NOT NULL
);

CREATE TABLE ProductGroups(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	ProductID VARCHAR(10) NOT NULL,
	GroupID int NOT NULL,
	FOREIGN KEY (ProductID) REFERENCES Products(ID) ON DELETE CASCADE,
	FOREIGN KEY (GroupID) REFERENCES Groups(ID)
);




select name from Categories where id in (select categoryid from Niches where id in (select nicheid from Products where name like '%diet%'))

update Categories set Icon = 'Health&Fitness.png'

alter table products alter column DigitalDownload bit not null
alter table products alter column Shippable bit not null
alter table products alter column German bit not null
alter table products alter column English bit not null
alter table products alter column Spanish bit not null
alter table products alter column French bit not null
alter table products alter column Italian bit not null
alter table products alter column Portuguese bit not null
alter table products alter column SinglePayment bit not null
alter table products alter column Subscription bit not null
alter table products alter column Trial bit not null

alter table products drop constraint [FK__Products__Vendor__2C1E8537]

drop table Vendors
create table Vendors(
	ID int primary key identity(1,1) not null,
	Name varchar(255) not null,
	APIKey varchar(255) not null
)

delete customers

ALTER TABLE Subscriptions ADD CONSTRAINT DF_Suspended DEFAULT 0 FOR Suspended

create table Subscriptions (
	ID varchar(10) primary key not null,
	CustomerID varchar(10) not null,
	NicheID int not null,
	Subscribed bit not null,
	Suspended bit default 0 not null,
	DateSubscribed datetime not null,
	DateUnsubscribed datetime null,
	FOREIGN KEY (CustomerID) REFERENCES Customers(ID) ON DELETE CASCADE,
	FOREIGN KEY (NicheID) REFERENCES Niches(ID)
)

alter table campaignrecords alter column subscriptionID varchar(10) not null
alter table campaignrecords add foreign key(subscriptionid) references subscriptions(ID) on delete cascade


drop table leadpages

create table LeadPages(
	ID varchar(10) NOT NULL PRIMARY KEY,
	Title varchar(255) not null,
	Body varchar(max) not null,
	PageTitle varchar(255) not null,
	NicheID int not null,
	foreign key (nicheid) references niches(id) on delete cascade
);


insert into Subscriptions ( CustomerID, NicheID, Subscribed, Suspended, DateSubscribed)
values('EEB4EDA0-ACEC-446A-8652-6EC622977B5D', 199, 1, 0, getdate())

SELECT DATEADD(day, DATEDIFF(day, 0, GETDATE()), 0)

drop trigger trgAfterInsertCampaignRecords

CREATE TRIGGER trgAfterInsertCampaignRecords ON Subscriptions 
FOR INSERT
AS
	declare @subscriptionId int;
	declare @nicheId int;
	
	

	select @subscriptionId=i.ID from inserted i;	
	select @nicheId=i.NicheId from inserted i;
	
	

	insert into CampaignRecords
           (SubscriptionID, Date, ProductID, Day) 
	select top 1 @subscriptionId, GETDATE(), id, 0
	from Products where NicheID = @nicheId
	

	

	
	










alter table campaignlogs add FOREIGN KEY (CustomerID) REFERENCES Customers(ID) ON DELETE CASCADE

alter table niches drop column emailid
alter table niches drop FK__Niches__EmailID__531856C7

create table SubscriptionLogs(
	SubscriptionID int not null,
	Date datetime not null,
	CurrentCampaignID int not null,
	CurrentDay int not null,
	primary key (SubscriptionID, Date),
	Foreign Key (CurrentCampaignID) References Campaigns(id)
)

alter table subscriptionLogs add Foreign Key (customerid) References customers(id)

alter table subscriptionLogs add CustomerID Uniqueidentifier
alter table subscriptionLogs alter column customerid Uniqueidentifier not null

select CampaignID, day from emails  as A where id in (select emailid from emailsentlogs where subscriptionid = 3) 


select id from emails where emails.CampaignID = A.CampaignID and emails.day > A.day



Create NonClustered Index IX_CampainID_Day on emails(campaignID, day)

update niches set name = replace(replace(Niches.Name,char(13),''),char(10),'')

alter table niches alter column leadpage varchar(255) not null

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


   alter table niches alter column emailid uniqueidentifier not null
   
   delete niches where categoryid = 18
   delete Categories where id = 18


   alter table niches alter column LeadMagnet varchar(255) not null

   update niches set leadmagnet = 'Lead Magnet'
   
	Create table EmailSentLog(
		EmailID uniqueidentifier not null,
		SentDate datetime not null,
		CustomerID uniqueidentifier not null,
		primary key(EmailID, SentDate, CustomerID),
		FOREIGN KEY (EmailID) REFERENCES emails(ID),
		FOREIGN KEY (CustomerID) REFERENCES Customers(ID)
	);

	alter table subscriptions add primary key(subscriptionid)
	alter table subscriptions drop [PK__Subscrip__91D1C12A8A2A5A62]


	
	select * from Transactions
	select * from LineItems
	select * from Upsells

	drop table Transactions
	drop table LineItems
	drop table Upsells

	

	select * from Customers

	Create table Transactions(
		id varchar(10) NOT NULL PRIMARY KEY,
		transactionTime datetime not null,
		receipt varchar(21) not null,
		transactionType varchar(15) not null,
		vendor varchar(10) not null,
		affiliate varchar(10) not null,
		role varchar(9) not null,
		totalAccountAmount float not null,
		paymentMethod varchar(4) not null,
		totalOrderAmount float not null,
		totalTaxAmount float not null,
		totalShippingAmount float not null,
		currency varchar(3) not null,
		orderLanguage varchar(2) not null,
		customerId varchar(10) NOT NULL,
		constraint uk_Transactions_receipt unique (receipt)
	);

	create table LineItems(
		transactionId varchar(10) NOT NULL,
		itemNo varchar(25) not null,
		productTitle varchar (255) not null,
		shippable bit not null,
		recurring bit not null,
		accountAmount float not null,
		quantity int not null,
		lineItemType varchar(8) not null,
		primary key(transactionId, itemNo),
		FOREIGN KEY (transactionId) REFERENCES Transactions(ID)
	);

	select * from Campaigns
	
	Create table Upsells(
		id varchar(10) NOT NULL PRIMARY KEY,
		upsellOriginalReceipt varchar(21) not null,
		upsellFlowId int not null,
		upsellSession varchar(16) not null,
		upsellPath varchar(12) not null,
		FOREIGN KEY (upsellOriginalReceipt) REFERENCES Transactions(receipt)
	);

	
