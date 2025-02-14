﻿using Relua.Deserialization.Literals;
using System.Collections.Generic;




namespace Relua.Deserialization.Expressions {

	/// <summary>
	/// Table constructor expression. Supports all forms of specifying table
	/// entries, including expression keys (`[expr] = value`), identifier keys
	/// (`id = value`) and sequential keys (`value`).
	/// 
	/// If the `AutofillSequentialKeysInTableConstructor` option
	/// in `Parser.Settings` is set to `true` (which it is by default),
	/// then sequential keys will automatically receive key fields of type
	/// `NumericLiteral` with the appropriate indices. For example, the table
	/// `{3, 2, 1}` will parse as three entries, all with `NumericLiteral` keys
	/// from 1 to 3.
	/// 
	/// Please note that Lua is perfectly fine with repeated keys in table
	/// constructors. Sequential keys can also be used anywhere, even after
	/// indexed keys, therefore the table `{1, 2, [3] = 4, 3}` will produce a
	/// table of length 3 with the values 1, 2, and 3.
	/// 
	/// ```
	/// {
	///     a = 1,
	///     b = 2,
	///     3,
	///     "x",
	///     [1 + 3] = true
	/// }
	/// ```
	/// </summary>
	public class TableConstructorExpression : Node, IExpression {

		/// <summary>
		/// Table constructor entry. If `ExplicitKey` is `true`, then the key
		/// for this `Entry` will always be emitted while writing the
		/// `TableConstructor`, even if it is a sequential table entry.
		/// 
		/// ```
		/// [3] = "value" -- index is NumberLiteral 3
		/// a = "value" -- index is StringLiteral a
		/// "value" -- index is null or sequential NumberLiteral depending on parser settings
		/// ```
		/// </summary>
		public class Entry : Node {

			public IExpression Key;
			public IExpression Value;
			public bool ExplicitKey;


			public void WriteIdentifierStyle(IndentAwareTextWriter writer, string index) {
				writer.Write(index);
				writer.Write(" = ");
				this.Value.Write(writer);
			}


			public void WriteGenericStyle(IndentAwareTextWriter writer) {
				writer.Write("[");
				this.Key.Write(writer);
				writer.Write("]");
				writer.Write(" = ");
				this.Value.Write(writer);
			}


			public override void Write(IndentAwareTextWriter writer) {
				this.Write(writer, false);
			}


			public void Write(IndentAwareTextWriter writer, bool skip_key) {
				if (skip_key || this.Key == null) {
					this.Value.Write(writer);
					return;
				}

				if (this.Key is StringLiteral && ((StringLiteral)this.Key).Value.IsIdentifier()) {
					this.WriteIdentifierStyle(writer, ((StringLiteral)this.Key).Value);
				} else {
					this.WriteGenericStyle(writer);
				}
			}


			public override void Accept(IVisitor visitor)
				=> visitor.Visit(this);

		}




		public List<Entry> Entries = new List<Entry>();


		public override void Write(IndentAwareTextWriter writer) {
			if (this.Entries.Count == 0) {
				writer.Write("{}");
				return;
			}

			if (this.Entries.Count == 1) {
				writer.Write("{ ");
				Entry ent = this.Entries[0];
				ent.Write(writer, skip_key: ent.Key is NumberLiteral && ((NumberLiteral)ent.Key).Value == 1 && !ent.ExplicitKey);
				writer.Write(" }");
				return;
			}

			int seq_idx = 1;

			writer.Write("{");
			writer.IncreaseIndent();
			for (int i = 0; i < this.Entries.Count; i++) {
				writer.WriteLine();

				Entry ent = this.Entries[i];

				bool is_sequential = false;
				if (ent.Key is NumberLiteral && ((NumberLiteral)ent.Key).Value == seq_idx && !ent.ExplicitKey) {
					is_sequential = true;
					seq_idx += 1;
				}

				this.Entries[i].Write(writer, skip_key: is_sequential);
				if (i < this.Entries.Count - 1) {
					writer.Write(",");
				}
			}
			writer.DecreaseIndent();
			writer.WriteLine();
			writer.Write("}");
		}


		public override void Accept(IVisitor visitor)
			=> visitor.Visit(this);
	}

}
