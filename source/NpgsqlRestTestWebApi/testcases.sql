create function case_returns_void_no_params() 
returns void 
language plpgsql
as 
$$
begin
    raise info 'case_returns_void_no_params';
end;
$$;

create function case_overloads() 
returns void 
language plpgsql
as 
$$
begin
    raise info 'case_overloads';
end;
$$;

create function case_overloads(_i int) 
returns void 
language plpgsql
as 
$$
begin
    raise info 'case_overloads = %', _i;
end;
$$;

create function case_overloads(_i int, _t text) 
returns void 
language plpgsql
as 
$$
begin
    raise info 'case_overloads = %, %', _i, _t;
end;
$$;