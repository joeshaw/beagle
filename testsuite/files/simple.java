/** 
*/

import java.sql. *;
import java.io. *;
import java.util. *;
import java.text. *;

public class DBAdmin {

  Connection conn;
  DatabaseFormatUtils databaseFormatUtils;
  Statement stmt;
  boolean inTransaction;
  boolean debugFlag = false;
  private final int areaCodeLength = 2;

    /** Default constructor.
      * Assumes my name/password and default machine/port etc.
      */
  public DBAdmin () {
    databaseFormatUtils = new DatabaseFormatUtils ();
    stmt = null;
    inTransaction = false;
  } 

  /** CS560 database but with different login/password
    * Create a new instance to use the CS560 database but for different account
    * @param name Account name of the oracle account
    * @param password Passwod for the account
    */ 
  public DBAdmin (String name, String password) {
  } 
  
  /** Closes the connection
    */ 
  public void close () {
    try {
      conn.close ();
    } catch (Exception ex) {	/* who cares if there is error while closing */
    }
  }				
  
  //Create a JDBC connection  
  void createConnection (
			   String server,
			   int port,
			   String driverName,
			   String accountName,
			   String password) {
    try {
      DriverManager.registerDriver (
				     new oracle.jdbc.driver.OracleDriver ());
      conn = DriverManager.getConnection (
					   "jdbc:oracle:thin:@" + server + ":" + String.valueOf (port) + ":" + driverName, accountName, password);
      System.out.println ("Connected");
    } catch (SQLException e) {
      System.out.println ("Unable to open SQL connection: " +
			  e.getMessage ());
      System.exit (0);
    }
    catch (Exception e) {
      System.err.println ("Some other error: " + e.getMessage ());
      System.exit (0);
    }
  }

  boolean startTransaction() {
	try {
		conn.setAutoCommit(false);
		stmt = conn.createStatement();
	} catch (Exception ex) { return false;}
	inTransaction = true;
  	return true;
  }
  
  boolean endTransaction() {
	  try{
		  conn.commit();
		  stmt.close();
		  conn.setAutoCommit(true);
	  } catch (Exception ex) { return false;}
	  inTransaction = false;
	  return true;
  }

  boolean cancelTransaction() {
	  try {
		  conn.rollback();
		  stmt.close();
		  conn.setAutoCommit(true);
	  } catch (Exception ex) { return false;}
	  inTransaction = true;
	  return true;
  }
  
  int addStudent (
		  String ID,
		  String fName,
		  String mName,
		  String lName,
		  char gender,
		  String SSN,
		  String street,
		  String city,
		  String state,
		  String ZIP,
		  String country,
		  String areaInterest,
		  boolean programType,
		  String sem,
		  int year,
		  String notes) {

	  /* check input parameters */
	  String updStr = "INSERT INTO phd_student(ID,first_name,middle_name,last_name,gender,SSN,street_address," + 
		  "city_address,state_address,ZIP_address,country_address,area_interest,program_type,start_sem," + 
		  "start_year,notes) VALUES('" + 
		  	ID + "','" +
			fName + "','" +
			(mName == null ? "" : mName) + "','" +
			lName + "','" +
			gender + "','" +
			SSN + "','" +
			street + "','" +
			city + "','" +
			state + "','" +
			ZIP + "','" +
			country + "','" +
			areaInterest + "'," + (programType ? 1 : 0) + ",'" +
			sem + "'," + year + ",'" +
			notes + "')";
	  System.out.println("DEBUG: addStudent : " + updStr);
    
	  try {
	  	if (!inTransaction) 
			stmt = conn.createStatement();
		stmt.executeUpdate (updStr);
          } catch (SQLException ex) {
		System.err.println ("addStudent error: " + ex.getMessage ());
		return -1 * (ex.getErrorCode ());
	  } finally {
	  	try {
	    		if (stmt != null &&  (!inTransaction))
				stmt.close ();
	  	} catch (Exception ex) {	/* do we care? */  }
	  }

	  return 0;
  }

  int addDegreeInfo(
		  String ID,
		  String university,
		  String degree,
		  int year) {
	  if (!databaseFormatUtils.checkUID(ID))
		  return 1;
	  if (!databaseFormatUtils.checkGeneralName(university))
		  return 2;
	  if (!databaseFormatUtils.checkDegree(degree))
		  return 3;
	  if (!databaseFormatUtils.checkYear(year))
		  return 1;
	  
	String updStr = "INSERT INTO degreee_info(ID,university,degree,date) VALUES('" +
		ID + "','" + university + "','" + degree + "'," + year + ")";
	System.out.println("DEBUG: in addDegreeInfo: " + updStr);

    try {
      if (!inTransaction) stmt = conn.createStatement ();
      stmt.executeUpdate (updStr);
    }
    catch (SQLException ex) {
      System.err.println ("addDegreeExam error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }
    finally {
      try {
	if (stmt != null &&  (!inTransaction))
	  stmt.close ();
      }
      catch (Exception ex) {	/* do we care? */
      }
    }
	  return 0;
  }
  
    /** Change advisor for a student.
      * Add a new entry in the advisor table.
      * @param studentID student UID
      * @param profID professor UID
      * @param startDate starting date of advising, should be valid format in YYYY-MM-DD 
      * @param endDate ending date of advising, should be valid format in YYYY-MM-DD
      * @param coFlag flag is 1 if co-advisor, else 0
      * @return 0 if successful, positive integers if certain argument
      *           is wrong, negative integers in other cases
      *
      */
  int changeAdvisor (
		      String studentID,
		      String profID,
		      String startDate,
		      String endDate,
		      boolean coFlag) {

    //check formats of the data
    if (!databaseFormatUtils.checkUID (studentID))
      return 1;
    if (!databaseFormatUtils.checkUID (profID))
      return 2;

    /* get the date values */
    //SimpleDateFormat dateFormat = new SimpleDateFormat("yyyy-MM-dd");
    //String sqlStartDate = "{d '" + dateFormat.format(startDate) + "'}";
    if (!databaseFormatUtils.checkDate(startDate))
	    return 3;
    String sqlStartDate = "(to_date('" + startDate + "','YYYY-MM-DD'))";

    if (!databaseFormatUtils.checkDate(endDate))
	    return 4;
    String sqlEndDate = (endDate == null ? "''" :
			 "(to_date('" + endDate + "','YYYY-MM-DD'))");

    //insert record into course table
    String updStr = "INSERT INTO Advising_history (ID,PID,START_DATE,END_DATE,CO_FLAG)" +
    "VALUES ('" + studentID + "','" +
    profID + "'," + sqlStartDate + "," + sqlEndDate + "," +
    (coFlag ? 1 : 0) + ")";
    // When adding new advising record set the end_date of the last advising record
    // to the current start date
    String oldAdvisingEndDateStr = "UPDATE Advising_history SET END_DATE = " + 
	    sqlStartDate + " WHERE ID=" + studentID + " AND END_DATE IS NULL";
      System.out.println (updStr);
      System.out.println (oldAdvisingEndDateStr);

    try {
      if (!inTransaction) stmt = conn.createStatement ();
      stmt.executeUpdate (updStr);
      stmt.executeUpdate (oldAdvisingEndDateStr);
    }
    catch (SQLException ex) {
      System.err.println ("changeAdvisor error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }
    finally {
      try {
	if (stmt != null && (!inTransaction))
	  stmt.close ();
      }
      catch (Exception ex) {	/* do we care? */
      }
    }

    return 0;
  }

  /** Adds information about some any area exam taken by some student.
   * Update student record to reflect this new information.
   * @return 0 if successful, positive integers if certain argument
   *           is wrong, negative integers in other cases
   *
   */ 
  int addOralExamInformation (
		  String ID,
		  String date,
		  String grade,
		  String notes) {
	  
	  if (!databaseFormatUtils.checkUID(ID))
		  return 1;
	  if (!databaseFormatUtils.checkDate(date))
		  return 2;
	  if (!databaseFormatUtils.checkGrade(grade))
		  return 3;

	  String updStr = "UPDATE TABLE phd_student SET " + 
		  "oral_exam_date=to_date('" + date + "','YYYY-MM-DD')," + 
		  "oral_exam_grade='" + grade + ",oral_exam_notes='" + notes +"' WHERE ID='" +
		  ID + "'";
	  System.out.println("DEBUG: in addOralExam: " + updStr);

    try {
      if (!inTransaction) stmt = conn.createStatement ();
      stmt.executeUpdate (updStr);
    }
    catch (SQLException ex) {
      System.err.println ("addOralExam error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }
    finally {
      try {
	if (stmt != null &&  (!inTransaction))
	  stmt.close ();
      }
      catch (Exception ex) {	/* do we care? */
      }
    }
	  
    return 0;
  } 

  /** Adds information about some any area exam taken by some student.
   * Update student record to reflect this new information.
   * params have been changed to ease input taking and error checking
   * @return 0 if successful, positive integers if certain argument
   *           is wrong, negative integers in other cases
   *
   */ 
  int addAreaExamInformation (
		  String ID,
		  String date,
		  String grade,
		  String notes,
		  String area) {
	  
	  if (!databaseFormatUtils.checkUID(ID))
		  return 1;
	  if (!databaseFormatUtils.checkArea(area))
		  return 5;
	  if (!databaseFormatUtils.checkDate(date))
		  return 2;
	  if (!databaseFormatUtils.checkGrade(grade))
		  return 3;

	  String updStr = "UPDATE TABLE phd_student SET area_exam_area = '" + 
		  area + "',area_exam_date=to_date('" + date + "','YYYY-MM-DD')," + 
		  "area_exam_grade='" + grade + ",area_exam_notes='" + notes +"' WHERE ID='" +
		  ID + "'";
	  System.out.println("DEBUG: in addAreaExam: " + updStr);

    try {
      if (!inTransaction) stmt = conn.createStatement ();
      stmt.executeUpdate (updStr);
    }
    catch (SQLException ex) {
      System.err.println ("addAreaExam error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }
    finally {
      try {
	if (stmt != null &&  (!inTransaction))
	  stmt.close ();
      }
      catch (Exception ex) {	/* do we care? */
      }
    }
	  
    return 0;
  } 
      
    /** Adds leave of absence information for a student. 
      * @return 0 if successful, positive integers if certain argument
      *           is wrong, negative integers in other cases
      *
      */ 
    int addLOA (
		      String sem,
		      String year,
		      String ID,
		      String notes) {

    //check formats of the data
    if (!databaseFormatUtils.checkUID (ID))
      return 3;

    // if (!databaseFormatUtils.checkYear(year))
    //              return 2;

    //insert record into course table
    String updStr = "INSERT INTO LOA VALUES('" + sem + "','" +
    year + "','" + ID + "','" + notes + "')";
      System.out.println (updStr);

    try {
      if (!inTransaction) stmt = conn.createStatement ();
      stmt.executeUpdate (updStr);
    }
    catch (SQLException ex) {
      System.err.println ("addLOA error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }
    finally {
      try {
	if (stmt != null &&  (!inTransaction))
	  stmt.close ();
      }
      catch (Exception ex) {	/* do we care? */
      }
    }

    return 0;

  }

  /** Adds information about a thesis defense faculty-student
	*/
  int addThesisProposalCommittee(
				  String ID,
				  String PID) {
    //check formats of the data
    if (!databaseFormatUtils.checkUID (ID))
      return 1;
    if (!databaseFormatUtils.checkUID (PID))
      return 2;

    // this should be called while in transaction - SANITY CHECK
    if (!inTransaction)
	    return -10000;
    
    String insertStr = "INSERT INTO thesis_proposal_committee(ID,PID) VALUES('" + 
	    ID + "','" + PID + "')";
    System.out.println(insertStr);

    try {
      stmt.executeUpdate (insertStr);
    }
    catch (SQLException ex) {
      System.err.println ("addThesisProposalCommittee error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }

    return 0;
  }
  
  /** Adds information about thesis defense - date title etc.
    */
  int addThesisProposalInfo(
		  String ID,
		  String title,
		  String date) {

    //check formats of the data
    if (!databaseFormatUtils.checkUID (ID))
      return 1;
    if (!databaseFormatUtils.checkGeneralName (title))
      return 2;
    if (!databaseFormatUtils.checkDate(date))
	return 3;

    String updateStr = "UPDATE PhD_student SET thesis_proposal_date = to_date('" + 
	    date + "','YYYY-MM-DD'),thesis_proposal_title='" + title + "' WHERE ID='" + ID + "'";
    System.out.println("DEBUG: in addThesisProposalInfo: " + updateStr);

    try {
      stmt.executeUpdate (updateStr);
    }
    catch (SQLException ex) {
      System.err.println ("addThesisProposalInfo error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }

    return 0;
  }

  /** Adds an external faculty information for thesis defense
    * While adding the record in the database, an ID is generated
    * Should be called while inTransaction is true
    * @param ID ID of the student who is defending his/her thesis
    * @param firstName faculty's first name
    * @param middleName middle name or initials
    * @param lastName last name
    * @param university name of university
    * @param department name of the faculty's department
    */
  int addExternalCommittee(
		  String ID,
		  String firstName,
		  String middleName,
		  String lastName,
		  String university,
		  String department) {
	int externalFacId;

    //check formats of the data
    if (!databaseFormatUtils.checkUID (ID))
      return 1;
    if (!databaseFormatUtils.checkName (firstName))
      return 2;
    if (!databaseFormatUtils.checkMiddleName (middleName))
      return 3;
    if (!databaseFormatUtils.checkName (lastName))
      return 4;
    if (!databaseFormatUtils.checkGeneralName (university))
      return 5;
    if (!databaseFormatUtils.checkGeneralName (department))
      return 6;

    // sanity check
    if (!inTransaction)
	    return -10000;
	
	boolean entryExists = false; // if the faculty already exists in the database

    // see if the external faculty member is already in the database
    try {
	String externalFacQStr = "SELECT external_fac_id FROM external_faculty where " + 
	        "university = '" + university +
	    	 "' AND department ='" + department + 
	       	 "' AND first_name = '" + firstName +
	        "' AND last_name = '" + lastName + 
	        (middleName == "" ? "" : "' AND middle_name = '" + middleName) + "'";
	System.out.println(externalFacQStr);
	ResultSet rs = stmt.executeQuery(externalFacQStr);
    	if (rs.next()) { //there is entry already - hopefully this is the only entry - no checking done now
		externalFacId = rs.getInt(1);
		System.out.println("external faculty exists, id=" + externalFacId);
		entryExists = true;
	} else {
    		//get the current value of external_fac_id
		// first close the previous result set
		rs.close();
		System.out.println("entry not found");
	   	String sequenceQStr = "SELECT external_fac_id.nextval from dual";
		rs = stmt.executeQuery(sequenceQStr);
		rs.next();
		externalFacId =	 rs.getInt(1);
		rs.close();
		System.out.println("next value of ext. fac. id is " + externalFacId);
	}
    } catch (SQLException ex) { 
	    System.out.println(ex.getMessage());
	    return (-1)*ex.getErrorCode();
    }
    
    //insert record into thesis_external_faculty_ table if not already present
	if (!entryExists) {
	    String updStr = "INSERT INTO external_faculty" + 
		    "(FIRST_NAME,MIDDLE_NAME,LAST_NAME,DEPARTMENT,UNIVERSITY,EXTERNAL_FAC_ID) VALUES('" + 
		    firstName + "','" +
		    middleName + "','" +
		    lastName + "','" +
		    department + "','" +
		    university + "'," +
		    externalFacId + ")";
	    System.out.println (updStr);
	
	    try {
	      stmt.executeUpdate (updStr);
	    }
	    catch (SQLException ex) {
	      System.err.println ("addLOA error: " + ex.getMessage ());
	      return -1 * (ex.getErrorCode ());
	    }
	}
	
    //XXX: dont care to rollback sequence if transaction is aborted in the middle
    //insert record into thesis_external_faculty
    String updStr = "INSERT INTO thesis_external_faculty VALUES('" + 
	    ID + "','" + externalFacId + "')";
    
    try {
      stmt.executeUpdate (updStr);
    }
    catch (SQLException ex) {
      System.err.println ("addLOA error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }

    return 0;
  }
		  
  /** Adds information about a thesis defense faculty-student
	*/
  int addThesisDefenseCommittee(
				  String ID,
				  String PID) {
    //check formats of the data
    if (!databaseFormatUtils.checkUID (ID))
      return 1;
    if (!databaseFormatUtils.checkUID (PID))
      return 2;

    // this should be called while in transaction - SANITY CHECK
    if (!inTransaction)
	    return -10000;
    
    String insertStr = "INSERT INTO thesis_defense_committee(ID,PID) VALUES('" + 
	    ID + "','" + PID + "')";
    System.out.println(insertStr);

    try {
      stmt.executeUpdate (insertStr);
    }
    catch (SQLException ex) {
      System.err.println ("addThesisDefenseCommittee error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }

    return 0;
  }
  
  /** Adds information about thesis defense - date title etc.
    */
  int addThesisDefenseInfo(
		  String ID,
		  String title,
		  String date) {

    //check formats of the data
    if (!databaseFormatUtils.checkUID (ID))
      return 1;
    if (!databaseFormatUtils.checkGeneralName (title))
      return 2;
    if (!databaseFormatUtils.checkDate(date))
	return 3;

    String updateStr = "UPDATE PhD_student SET thesis_defense_date = to_date('" + 
	    date + "','YYYY-MM-DD'),thesis_defense_title='" + title + "' WHERE ID='" + ID + "'";
    System.out.println("DEBUG: in addThesisDefenseInfo: " + updateStr);

    try {
      stmt.executeUpdate (updateStr);
    }
    catch (SQLException ex) {
      System.err.println ("addThesisDefenseInfo error: " + ex.getMessage ());
      return -1 * (ex.getErrorCode ());
    }

    return 0;
  }
}

