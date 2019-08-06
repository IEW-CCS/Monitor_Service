using System;
using System.Collections.Generic;
using System.Linq;
//using System.Data.Entity;
//using System.Data.Entity.ModelConfiguration.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.EntityFrameworkCore;
using System.Data.Common;
using MonitorService.Model;

namespace MonitorService.Model
{
    
    public class IOT_DbContext : DbContext
    {

        static DbContextOptions CreateDbConnection(string providerName, string connectionString)
        {

            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();
            switch (providerName)
            {
            case "MS SQL":
                optionsBuilder.UseSqlServer(connectionString);
                break;

            case "My SQL":
                optionsBuilder.UseMySQL(connectionString);
                break;

            default:
                break;
            }

            return optionsBuilder.Options;
        }

        // Constructor 
        public IOT_DbContext(string provider, string connectstring) : base(CreateDbConnection(provider, connectstring))
        {

        }

        public DbSet<IOT_STATUS_MONITOR> IOT_STATUS_MONITOR { get; set; }

        /*
         public DbSet<IOT_ALERT> IOT_ALERT { get; set; }
         public DbSet<IOT_DEVICE> IOT_DEVICE { get; set; }
         public DbSet<IOT_DEVICE_TYPE> IOT_DEVICE_TYPE { get; set; }
         public DbSet<IOT_DEVICE_SPEC> IOT_DEVICE_SPEC { get; set; }
         public DbSet<IOT_LOCATION> IOT_LOCATION { get; set; }
         public DbSet<IOT_EDC_GLS_INFO> IOT_EDC_GLS_INFO { get; set; }
         public DbSet<IOT_GATEWAY> IOT_GATEWAY { get; set; }
         public DbSet<IOT_TAG_SET> IOT_TAG_SET { get; set; }
         public DbSet<IOT_DEVICE_TAG> IOT_DEVICE_TAG { get; set; }
         public DbSet<IOT_DEVICE_CALC_TAG> IOT_DEVICE_CALC_TAG { get; set; }
         public DbSet<IOT_TAG> IOT_TAG { get; set; }
         public DbSet<IOT_CALC_TAG> IOT_CALC_TAG { get; set; }
         public DbSet<IOT_EDC_HEADER_SET> IOT_EDC_HEADER_SET { get; set; }
         public DbSet<IOT_EDC_HEADER> IOT_EDC_HEADER { get; set; }
         public DbSet<IOT_EDC_XML_CONF> IOT_EDC_XML_CONF { get; set; }

         public DbSet<IOT_DEVICE_EDC_LABEL> IOT_DEVICE_EDC_LABEL { get; set; }
         public DbSet<IOT_DEVICE_EDC> IOT_DEVICE_EDC { get; set; }
        */
    }
}