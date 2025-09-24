-- Aggiorna la dimensione del campo ProfilePicture nella tabella Users
ALTER TABLE "Users" ALTER COLUMN "ProfilePicture" TYPE character varying(1048576);
